using System;
using System.AddIn.Contract;
using System.AddIn.Pipeline;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Windows;
using System.Xml;
using PipelineHints;

namespace PipelineBuilder
{
	/// <summary>
	/// The pipeline builder 
	/// </summary>
	public class PipelineBuilder : MarshalByRefObject
	{
		private PipelineSegmentSource _Aib;
		private PipelineSegmentSource _Asa;
		private String _AsmPath;
		private Assembly _ContractAsm;
		private PipelineSegmentSource _Hav;
		private PipelineSegmentSource _Hsa;
		private SymbolTable _Symbols;
		private Dictionary<Type, List<Type>> _TypeHierarchy;
		private PipelineSegmentSource _view;
		private Dictionary<string, IList<CodeCommentStatement>> _XmlComments;

		public PipelineBuilder(String assemblyPath)
		{
			_AsmPath = assemblyPath;
		}

		public PipelineBuilder(String assemblyPath, bool newDomain)
		{
			init(assemblyPath, newDomain);
		}

		private void init(String assemblyPath, bool newDomain)
		{
			_AsmPath = assemblyPath;

			if (!newDomain)
			{
				_ContractAsm = Assembly.LoadFrom(assemblyPath);
			}
		}

		private List<PipelineSegmentSource> buildRemotePipeline()
		{
			String baseDir = typeof (PipelineBuilder).Assembly.Location;
			baseDir = baseDir.Substring(0, baseDir.LastIndexOf("\\"));
			AppDomain remoteDomain = AppDomain.CreateDomain("WorkerDomain", null, baseDir, null, true);
			List<PipelineSegmentSource> source;

			try
			{
				var remoteBuilder =
					(PipelineBuilder)
					remoteDomain.CreateInstanceAndUnwrap(typeof (PipelineBuilder).Assembly.FullName,
					                                     typeof (PipelineBuilder).FullName,
					                                     false, BindingFlags.Public | BindingFlags.Instance, null,
					                                     new object[] {_AsmPath, false}, null, null, null);
				source = remoteBuilder.BuildPipeline();
			}
			finally
			{
				AppDomain.Unload(remoteDomain);
			}

			return source;
		}

		/// <summary>
		/// Loads the XML comments (if they exist) for the assembly
		/// </summary>
		/// <param name="dllName">Name of the DLL.</param>
		/// <returns>Any XML comments loaded</returns>
		private Dictionary<string, IList<CodeCommentStatement>> LoadXMLComments(string dllName)
		{
			var xmlComments = new Dictionary<string, IList<CodeCommentStatement>>();
			var x = new Uri(dllName);
			string xmlDoc = x.LocalPath.Replace(".dll", ".xml");
			if (!File.Exists(xmlDoc))
			{
				return xmlComments;
			}

			var theDoc = new XmlDocument();
			theDoc.Load(xmlDoc);
			XmlNodeList theList = theDoc.GetElementsByTagName("member");

			foreach (XmlNode node in theList)
			{
				var listComments = new List<CodeCommentStatement>();
				string[] lines = node.InnerXml.Split('\n');
				foreach (string line in lines)
				{
					string res = line.Replace('\r', ' ');
					res = res.Replace('\t', ' ');
					while (res.IndexOf("  ") != -1)
					{
						res = res.Replace("  ", " ");
					}
					listComments.Add(new CodeCommentStatement(res, true));
				}
				xmlComments.Add(node.Attributes[0].Value, listComments);
			}
			return xmlComments;
		}

		public List<PipelineSegmentSource> BuildPipeline()
		{
			//If we haven't loaded the contract assembly yet it means we want to avoid loading the contract assembly in this domain
			//and should do it remotely. Once we're in the new domain we'll have loaded the contract asm and we'll fall through this
			//and do the work.
			if (_ContractAsm == null)
			{
				List<PipelineSegmentSource> source = buildRemotePipeline();
				return source;
			}
			_Symbols = new SymbolTable(_ContractAsm);
			_XmlComments = LoadXMLComments(_ContractAsm.CodeBase);
			_TypeHierarchy = new Dictionary<Type, List<Type>>();
			var components = new List<PipelineSegmentSource>();
			_Aib = new PipelineSegmentSource(SegmentType.AddInView, _Symbols);
			_Asa = new PipelineSegmentSource(SegmentType.AddInSideAdapter, _Symbols);
			_Hsa = new PipelineSegmentSource(SegmentType.HostSideAdapter, _Symbols);
			_Hav = new PipelineSegmentSource(SegmentType.HostAddInView, _Symbols);
			_view = new PipelineSegmentSource(SegmentType.View, _Symbols);
			components.Add(_Asa);
			components.Add(_Hsa);


			//If the contract assembly is marked as having the views shared we should add a generic "view" component.
			//If not then we build both add-in side and host-side view source files. 
			
			if (ShouldShareViews()) components.Add(_view);
			else 
			{
				components.Add(_Aib);
				components.Add(_Hav);
			}

			//Parse the type hierarchy in the contract so we know how to express it in the views. 
			BuildUpCastableTypeHierarchy();

			//Iterate through all of the contract types
			foreach (var t in _ContractAsm.GetExportedTypes())
			{
				//Check to see if the type is a contract
				if (typeof (IContract).IsAssignableFrom(t)) handleContract(t);
				else handleNonContract(t);
			}

			return components;
		}

		private void handleContract(Type t)
		{
			bool activatable =
				t.GetCustomAttributes(false)
				.NotAllTrue(atr => !atr.GetType().Equals(typeof (AddInContractAttribute)));

			PipelineSegment customSettings = PipelineSegment.None;
			if (t.GetCustomAttributes(typeof (CustomPipelineAttribute), false).Length > 0)
			{
				customSettings =
					((CustomPipelineAttribute) t.GetCustomAttributes(typeof (CustomPipelineAttribute), false)[0]).Segment;
			}
			
			//Build host, add-in views, and shared views
			if ((customSettings & PipelineSegment.AddInView) != PipelineSegment.AddInView)
				BuildView(t, _Aib, SegmentType.AddInView, activatable, _XmlComments);
			
			if ((customSettings & PipelineSegment.HostView) != PipelineSegment.HostView)
				BuildView(t, _Hav, SegmentType.HostAddInView, activatable, _XmlComments);
			
			if ((customSettings & PipelineSegment.Views) != PipelineSegment.Views)
				BuildView(t, _view, SegmentType.View, activatable, _XmlComments);

			//Build add-in side adapters
			if ((customSettings & PipelineSegment.AddInSideAdapter) != PipelineSegment.AddInSideAdapter)
			{
				BuildViewToContractAdapter(t, _Asa, SegmentType.AddInSideAdapter, activatable);
				BuildContractToViewAdapter(t, _Asa, SegmentType.AddInSideAdapter, false);
			}

			BuildStaticAdapters(t, _Asa, SegmentType.AddInSideAdapter);

			//Build host side adapters
			if ((customSettings & PipelineSegment.HostSideAdapter) != PipelineSegment.HostSideAdapter)
			{
				BuildViewToContractAdapter(t, _Hsa, SegmentType.HostSideAdapter, false);
				BuildContractToViewAdapter(t, _Hsa, SegmentType.HostSideAdapter, activatable);
			}

			BuildStaticAdapters(t, _Hsa, SegmentType.HostSideAdapter);
		}

		private void handleNonContract(Type t)
		{
			if (t.IsEnum)
			{
				//If type is an enum build adapters and view for that
				BuildEnumView(t, _Aib, SegmentType.AddInView, _XmlComments);
				BuildEnumView(t, _Hav, SegmentType.HostAddInView, _XmlComments);
				BuildEnumView(t, _view, SegmentType.View, _XmlComments);
				BuildStaticAdapters(t, _Hsa, SegmentType.HostSideAdapter);
				BuildStaticAdapters(t, _Asa, SegmentType.AddInSideAdapter);
				BuildStaticAdapters(t.MakeArrayType(), _Hsa, SegmentType.HostSideAdapter);
				BuildStaticAdapters(t.MakeArrayType(), _Asa, SegmentType.AddInSideAdapter);
			}
			else if (t.IsValueType)
			{
				ValidateStructContract(t);
				PipelineSegment customSettings = PipelineSegment.None;
				if (t.GetCustomAttributes(typeof (CustomPipelineAttribute), false).Length > 0)
				{
					customSettings =
						((CustomPipelineAttribute) t.GetCustomAttributes(typeof (CustomPipelineAttribute), false)[0]).Segment;
				}
				//Build host, add-in views, and shared views
				if ((customSettings & PipelineSegment.AddInView) != PipelineSegment.AddInView)
				{
					BuildStructView(t, _Aib, SegmentType.AddInView, _XmlComments);
				}
				if ((customSettings & PipelineSegment.HostView) != PipelineSegment.HostView)
				{
					BuildStructView(t, _Hav, SegmentType.HostAddInView, _XmlComments);
				}
				if ((customSettings & PipelineSegment.Views) != PipelineSegment.Views)
				{
					BuildStructView(t, _view, SegmentType.View, _XmlComments);
				}
				Type arrayVersion = t.MakeArrayType();
				//Build add-in side adapters
				if ((customSettings & PipelineSegment.AddInSideAdapter) != PipelineSegment.AddInSideAdapter)
				{
					BuildStaticAdapters(t, _Asa, SegmentType.AddInSideAdapter);
				}
				BuildStaticAdapters(arrayVersion, _Asa, SegmentType.AddInSideAdapter);
				//Build host side adapters
				if ((customSettings & PipelineSegment.HostSideAdapter) != PipelineSegment.HostSideAdapter)
				{
					BuildStaticAdapters(t, _Hsa, SegmentType.HostSideAdapter);
				}
				BuildStaticAdapters(arrayVersion, _Hsa, SegmentType.HostSideAdapter);
			}
		}


		private void ValidateStructContract(Type t)
		{
			if (t.GetConstructors().Length != 1)
			{
				throw new InvalidOperationException("Struct contracts must have exactly one constructor: " + t.FullName);
			}
			if ((t.Attributes & TypeAttributes.Serializable) != TypeAttributes.Serializable)
			{
				throw new InvalidOperationException("Struct contracts must be marked with the SerializableAttribute: " + t.FullName);
			}
			if (t.GetConstructors()[0].GetParameters().Length != t.GetProperties().Length)
			{
				throw new InvalidOperationException("Struct contracts must have matching properties and constructor parameters:" +
				                                    t.FullName);
			}
			foreach (PropertyInfo pi in t.GetProperties())
			{
				if (pi.GetGetMethod() == null)
				{
					throw new InvalidOperationException("Stuct contracts must  have 'get' accessors for each property: " + t.FullName +
					                                    "." + pi.Name);
				}
				ConstructorInfo ci = t.GetConstructors()[0];
				bool foundMatch = false;
				foreach (ParameterInfo param in ci.GetParameters())
				{
					if (ConvertParamNameToProperty(param.Name).Equals(pi.Name))
					{
						if (param.ParameterType.Equals(pi.PropertyType))
						{
							foundMatch = true;
							break;
						}
						else
						{
							throw new InvalidOperationException(
								"Struct contracts constructor param names and types must correspond to a property: " + t.FullName + "," +
								param.Name);
						}
					}
				}
				if (!foundMatch)
				{
					throw new InvalidOperationException(
						"Struct contracts  property names and types must correspond to a constructor parameter: " + t.FullName + "," +
						pi.Name);
				}
			}
		}

		private bool ShouldShareViews()
		{
			return _ContractAsm.GetCustomAttributes(typeof (ShareViews), false).Length > 0;
		}

		private SegmentType GetViewType(SegmentType adapterType)
		{
			if (ShouldShareViews())
				return SegmentType.View;
			if (adapterType.Equals(SegmentType.AddInSideAdapter))
				return SegmentType.AddInView;
			if (adapterType.Equals(SegmentType.HostSideAdapter))
				return SegmentType.HostAddInView;

			throw new InvalidOperationException("Must pass in an adapter segment type");
		}


		private void BuildUpCastableTypeHierarchy()
		{
			foreach (Type t in _ContractAsm.GetExportedTypes())
			{
				if (!typeof (IContract).IsAssignableFrom(t)) continue;

				Type baseContract = GetBaseContract(t);

				if (baseContract == null || !IsUpCastable(baseContract)) continue;

				List<Type> siblings;

				if (_TypeHierarchy.TryGetValue(baseContract, out siblings))
				{
					siblings.Add(t);
				}
				else
				{
					siblings = new List<Type>();
					siblings.Add(t);
					_TypeHierarchy.Add(baseContract, siblings);
				}
			}
		}

		internal bool IsUpCastable(Type t)
		{
			return t.GetCustomAttributes(typeof (AllowViewUpCasting), false).Length > 0;
		}

		/// <summary>
		/// Build the view for an enum. This essentially creates a mirror image of the enum and it's values using the proper names. 
		/// </summary>
		/// <param name="contractType">Input enum type</param>
		/// <param name="component">The pipeline component this source should be part of</param>
		/// <param name="viewType">Either AddInView or HostAddInView</param>
		internal void BuildEnumView(Type contractType, PipelineSegmentSource component, SegmentType viewType,
		                            Dictionary<string, IList<CodeCommentStatement>> _xmlComments)
		{
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(viewType, contractType));
			var type = new CodeTypeDeclaration(_Symbols.GetNameFromType(contractType, viewType, SegmentDirection.None, false));
			type.Attributes = MemberAttributes.Public;
			type.IsEnum = true;
			if (_xmlComments.ContainsKey(GetXMLCommentKeyString(type, contractType.Namespace)))
			{
				foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(type, contractType.Namespace)])
				{
					type.Comments.Add(ccs);
				}
			}
			foreach (FieldInfo fi in contractType.GetFields())
			{
				if (!fi.Name.Equals("value__"))
				{
					var field = new CodeMemberField(fi.FieldType, fi.Name);
					field.InitExpression = new CodePrimitiveExpression(fi.GetRawConstantValue());
					if (_xmlComments.ContainsKey(GetXMLCommentKeyString(field, fi.FieldType.FullName)))
					{
						foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(field, fi.FieldType.FullName)])
						{
							field.Comments.Add(ccs);
						}
					}
					type.Members.Add(field);
				}
			}
			if (contractType.GetCustomAttributes(typeof (FlagsAttribute), false).Length > 0)
			{
				type.CustomAttributes.Add(new CodeAttributeDeclaration("System.Flags"));
			}
			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(type.Name, ccu));
		}

		internal void BuildStructView(Type contractType, PipelineSegmentSource component, SegmentType viewType,
		                              Dictionary<string, IList<CodeCommentStatement>> _xmlComments)
		{
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(viewType, contractType));
			var type = new CodeTypeDeclaration(_Symbols.GetNameFromType(contractType, viewType, SegmentDirection.None, false));
			type.Attributes = MemberAttributes.Public;
			type.IsStruct = true;
			foreach (PropertyInfo pi in contractType.GetProperties())
			{
				var prop = new CodeMemberProperty();
				prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				prop.Name = pi.Name;
				prop.HasGet = true;
				prop.HasSet = false;
				prop.Type = GetViewTypeReference(viewType, pi.PropertyType, contractType, SegmentDirection.None);
				prop.GetStatements.Add(
					new CodeMethodReturnStatement(
						new CodeVariableReferenceExpression(ConvertNameToField(pi.Name))));
				if (pi.GetSetMethod() != null)
				{
					prop.SetStatements.Add(
						new CodeAssignStatement(
							new CodeVariableReferenceExpression(ConvertNameToField(pi.Name)),
							new CodePropertySetValueReferenceExpression()));
				}
				type.Members.Add(prop);
			}
			var constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public;
			foreach (ParameterInfo pi in contractType.GetConstructors()[0].GetParameters())
			{
				var param = new CodeParameterDeclarationExpression();
				param.Name = pi.Name;
				param.Type = GetViewTypeReference(viewType, pi.ParameterType, contractType, SegmentDirection.None);
				constructor.Parameters.Add(param);
				var field = new CodeMemberField();
				field.Name = ConvertNameToField(pi.Name);
				field.Type = param.Type;
				type.Members.Add(field);
				constructor.Statements.Add(
					new CodeAssignStatement(
						new CodeVariableReferenceExpression(field.Name),
						new CodeVariableReferenceExpression(pi.Name)));
			}
			type.Members.Add(constructor);


			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(type.Name, ccu));
		}

		private CodeMemberMethod CreateStructContractToViewStaticAdapter(Type contractType, SegmentType componentType,
		                                                                 CodeTypeReference viewType)
		{
			CodeMemberMethod result = CreateStructStaticAdapter(contractType, new CodeTypeReference(contractType), viewType,
			                                                    "contract", componentType, SegmentDirection.ContractToView);
			result.Name = "ContractToViewAdapter";
			return result;
		}

		private CodeMemberMethod CreateStructViewtoContractStaticAdapter(Type contractType, SegmentType componentType,
		                                                                 CodeTypeReference viewType)
		{
			CodeMemberMethod result = CreateStructStaticAdapter(contractType, viewType, new CodeTypeReference(contractType),
			                                                    "view", componentType, SegmentDirection.ViewToContract);
			result.Name = "ViewToContractAdapter";
			return result;
		}

		private CodeMemberMethod CreateStructStaticAdapter(Type contractType, CodeTypeReference source,
		                                                   CodeTypeReference destination, String paramName,
		                                                   SegmentType segment, SegmentDirection direction)
		{
			var adapter = new CodeMemberMethod();
			adapter.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			adapter.ReturnType = destination;
			adapter.Parameters.Add(
				new CodeParameterDeclarationExpression(source, paramName));
			var constructor = new CodeObjectCreateExpression();
			constructor.CreateType = destination;
			foreach (ParameterInfo pi in contractType.GetConstructors()[0].GetParameters())
			{
				var prop =
					new CodePropertyReferenceExpression(
						new CodeVariableReferenceExpression(paramName),
						ConvertParamNameToProperty(pi.Name));
				if (!TypeNeedsAdapting(pi.ParameterType))
				{
					constructor.Parameters.Add(prop);
				}
				else
				{
					constructor.Parameters.Add(
						CallStaticAdapter(segment, pi.ParameterType, prop, direction));
				}
			}
			adapter.Statements.Add(
				new CodeMethodReturnStatement(constructor));
			return adapter;
		}

		private CodeMemberMethod CreateArrayContractToViewStaticAdapter(Type contractType, SegmentType componentType,
		                                                                CodeTypeReference viewType)
		{
			CodeMemberMethod result = CreateArrayStaticAdapter(contractType, new CodeTypeReference(contractType), viewType,
			                                                   "contract", componentType, SegmentDirection.ContractToView);
			result.Name = "ContractToViewAdapter";
			return result;
		}

		private CodeMemberMethod CreateArrayViewToContractStaticAdapter(Type contractType, SegmentType componentType,
		                                                                CodeTypeReference viewType)
		{
			CodeMemberMethod result = CreateArrayStaticAdapter(contractType, viewType, new CodeTypeReference(contractType),
			                                                   "view", componentType, SegmentDirection.ViewToContract);
			result.Name = "ViewToContractAdapter";
			return result;
		}


		private CodeMemberMethod CreateArrayStaticAdapter(Type contractType, CodeTypeReference source,
		                                                  CodeTypeReference destination, String paramName, SegmentType segment,
		                                                  SegmentDirection direction)
		{
			var adapter = new CodeMemberMethod();
			adapter.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			adapter.ReturnType = destination;
			adapter.Parameters.Add(
				new CodeParameterDeclarationExpression(source, paramName));
			var result = new CodeVariableDeclarationStatement(destination, "result");
			var input = new CodeVariableReferenceExpression(paramName);

			var nullContractCheck = new CodeConditionStatement();
			nullContractCheck.Condition = new CodeBinaryOperatorExpression(input, CodeBinaryOperatorType.IdentityEquality,
			                                                               new CodePrimitiveExpression(null));
			nullContractCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
			adapter.Statements.Add(nullContractCheck);


			result.InitExpression = new CodeArrayCreateExpression(destination,
			                                                      new CodePropertyReferenceExpression(input, "Length"));
			//for (int i = 0;i < paramName.Length;i = i + 1)
			var init = new CodeIterationStatement();
			init.InitStatement = new CodeVariableDeclarationStatement(typeof (int), "i", new CodePrimitiveExpression(0));
			var i = new CodeVariableReferenceExpression("i");
			init.IncrementStatement = new CodeAssignStatement(i,
			                                                  new CodeBinaryOperatorExpression(i, CodeBinaryOperatorType.Add,
			                                                                                   new CodePrimitiveExpression(1)));
			init.TestExpression = new CodeBinaryOperatorExpression(i, CodeBinaryOperatorType.LessThan,
			                                                       new CodePropertyReferenceExpression(input, "Length"));
			//result[i] = SourceAdapter.DirectionAdapter(input[i])
			var valInit = new CodeAssignStatement();
			valInit.Left = new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("result"), i);
			valInit.Right = CallStaticAdapter(segment, contractType.GetElementType(), new CodeArrayIndexerExpression(input, i),
			                                  direction);
			init.Statements.Add(valInit);
			var ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("result"));
			adapter.Statements.Add(result);
			adapter.Statements.Add(init);
			adapter.Statements.Add(ret);
			return adapter;
		}

		internal static String ConvertPropertyNametoParameter(String name)
		{
			return name[0].ToString().ToLower() + name.Substring(1);
		}

		internal static String ConvertNameToField(String name)
		{
			return "_" + ConvertPropertyNametoParameter(name);
		}


		internal static String ConvertParamNameToProperty(String name)
		{
			return name[0].ToString().ToUpper() + name.Substring(1);
		}


		internal static bool IsNativeHandle(Type t)
		{
			return (t.Equals(typeof (INativeHandleContract)));
		}

		internal static CodeTypeReference GetNativeHandleViewType(SegmentDirection direction)
		{
			return new CodeTypeReference(typeof (FrameworkElement));
		}

		/// <summary>
		/// Build the view types for a standard contract
		/// </summary>
		/// <param name="contractType">Type of input contract</param>
		/// <param name="component">Pipeline component the source should be added to</param>
		/// <param name="componentType">Pipeline view type (hav or AddInView or generic) </param>
		/// <param name="activatable">Is this type an activatable contract</param>
		internal void BuildView(Type contractType, PipelineSegmentSource component, SegmentType componentType,
		                        bool activatable, Dictionary<string, IList<CodeCommentStatement>> _xmlComments)
		{
			if (IsEvent(contractType))
			{
				//Contract type is an event contract and does not have a corresponding view
				return;
			}
			String typeName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, false);
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(componentType, contractType));
			var type = new CodeTypeDeclaration(typeName);
			type.TypeAttributes = TypeAttributes.Abstract | TypeAttributes.Public;
			object[] typeComments = contractType.GetCustomAttributes(typeof (CommentAttribute), false);
			foreach (CommentAttribute comment in typeComments)
			{
				type.Comments.Add(new CodeCommentStatement(comment.Comment, true));
			}
			if (_xmlComments.ContainsKey(GetXMLCommentKeyString(contractType, contractType.Namespace)))
			{
				foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(contractType, contractType.Namespace)])
				{
					type.Comments.Add(ccs);
				}
			}
			if (IsViewInterface(contractType))
			{
				type.TypeAttributes |= TypeAttributes.Interface;
			}

			//This will consult the type hierarchy we built earlier, currently we only support one base type or 1 implemented interface
			Type baseType = GetBaseContract(contractType);
			if (baseType != null)
			{
				var baseRef =
					new CodeTypeReference(_Symbols.GetNameFromType(baseType, componentType, SegmentDirection.None, contractType));
				type.BaseTypes.Add(baseRef);
			}
			if (IsEventArgs(contractType))
			{
				//Contract type is an event args type and needs it as a base
				EventArgsAttribute argsType = GetEventArgs(contractType);
				if (argsType.Cancelable)
				{
					type.BaseTypes.Add(typeof (CancelEventArgs));
				}
				else
				{
					type.BaseTypes.Add(typeof (EventArgs));
				}
			}
			//Only the add-in base and shared views need an attribute, the HostAddInView doesn't need one. 
			if (activatable && (componentType == SegmentType.AddInView || componentType == SegmentType.View))
			{
				var marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof (AddInBaseAttribute)));
				type.CustomAttributes.Add(marker);
			}
			var props = new Dictionary<string, CodeMemberProperty>();
			foreach (MethodInfo mi in GetMethodsFromContract(contractType, false))
			{
				//We only need to build the event once, so we decided to do it on event add.
				//We do not do error checking to match up adds and removes.
				if (IsEventAdd(mi))
				{
					CodeTypeReference eventType = GetEventViewType(componentType, mi, false);
					var abstractEvent = new CodeMemberEvent();
					EventAddAttribute attr = GetEventAdd(mi);
					//TODO: remove this line. Abstract events are not supported by codedom since VB can't handle them.
					abstractEvent.Attributes = MemberAttributes.Abstract;
					abstractEvent.Name = attr.Name;
					abstractEvent.Type = eventType;
					type.Members.Add(abstractEvent);
					//We only look for comments on the event add method. Any comments on the event remove method will be ignored
					object[] eventComments = mi.GetCustomAttributes(typeof (CommentAttribute), false);
					foreach (CommentAttribute comment in eventComments)
					{
						abstractEvent.Comments.Add(new CodeCommentStatement(comment.Comment));
					}
					if (_xmlComments.ContainsKey(GetXMLCommentKeyString(abstractEvent, contractType.FullName)))
					{
						foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(abstractEvent, contractType.FullName)])
						{
							abstractEvent.Comments.Add(ccs);
						}
					}
					continue;
				}
				if (IsEventRemove(mi))
				{
					continue;
				}
				//If this method is marked as a property method using an attribute or declared directly as a property in the contrac then
				//we should express it as a property in the view, rather than as a method.
				if (IsProperty(mi))
				{
					CodeMemberProperty prop;

					SegmentDirection direction = SegmentDirection.None;
					bool prefix = false;
					prop = GetProperyDecl(contractType, type, mi, props, componentType, direction, prefix);
					switch (GetPropertyAttribute(mi).Type)
					{
						case PropertyType.set:
							prop.HasSet = true;
							break;
						case PropertyType.get:
							prop.HasGet = true;
							break;
					}
					object[] propComments = mi.GetCustomAttributes(typeof (CommentAttribute), false);
					foreach (CommentAttribute comment in propComments)
					{
						prop.Comments.Add(new CodeCommentStatement(comment.Comment));
					}
					if (_xmlComments.ContainsKey(GetXMLCommentKeyString(prop, contractType.FullName)))
					{
						foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(prop, contractType.FullName)])
						{
							prop.Comments.Add(ccs);
						}
					}
					continue;
				}
				var method = new CodeMemberMethod();
				method.Attributes = MemberAttributes.Abstract | MemberAttributes.Public;
				method.Name = mi.Name;
				//Setup the return type for this member in the view
				method.ReturnType = GetViewTypeReference(componentType, mi.ReturnType, contractType, SegmentDirection.None);
				//For each parameter in the method in the contract, add the right one to the view
				AddParametersToViewMethod(componentType, mi, method);
				object[] methodComments = mi.GetCustomAttributes(typeof (CommentAttribute), false);
				foreach (CommentAttribute comment in methodComments)
				{
					method.Comments.Add(new CodeCommentStatement(comment.Comment));
				}
				if (_xmlComments.ContainsKey(GetXMLCommentKeyString(method, contractType.FullName)))
				{
					foreach (CodeCommentStatement ccs in _xmlComments[GetXMLCommentKeyString(method, contractType.FullName)])
					{
						method.Comments.Add(ccs);
					}
				}
				type.Members.Add(method);
			}
			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(typeName, ccu));
		}

		private static string GetXMLCommentKeyString(object value, string contractName)
		{
			/* Type     Classes, delegates          T:
               Field    Member variables            F: 
               Method   Procedures and functions    M:
               Property Properties                  P:
               Event    Events                      E:*/
			if (value is Type)
			{
				return "T:" + ((Type) value).FullName;
			}

			if (value is CodeTypeDeclaration)
			{
				return "T:" + contractName + "." + ((CodeTypeDeclaration) value).Name;
			}
			if (value is CodeMemberMethod)
			{
				// {[M:MSDataSourceContracts.MSDataSourceContract.DeviceChange(System.IntPtr,System.IntPtr,System.Int32,System.IntPtr)
				var methodTitle = new StringBuilder();
				var cmm = value as CodeMemberMethod;
				if (cmm != null)
				{
					methodTitle.Append("M:");
					methodTitle.Append(contractName);
					methodTitle.Append(".");
					methodTitle.Append(((CodeMemberMethod) value).Name);
					methodTitle.Append("(");
					bool firstParameter = true;
					foreach (CodeParameterDeclarationExpression param in cmm.Parameters)
					{
						if (firstParameter)
						{
							firstParameter = false;
						}
						else
						{
							methodTitle.Append(",");
						}
						methodTitle.Append(param.Type.BaseType);
					}
					methodTitle.Append(")");
				}
				return methodTitle.ToString();
			}
			if (value is CodeMemberProperty)
			{
				return "P:" + contractName + "." + ((CodeMemberProperty) value).Name;
			}
			if (value is CodeMemberEvent)
			{
				return "E:" + contractName + "." + ((CodeMemberEvent) value).Name;
			}
			if (value is CodeMemberField)
			{
				return "F:" + contractName + "." + ((CodeMemberField) value).Name;
			}

			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// This method adds paramters to an individual method on a view type
		/// </summary>
		/// <param name="componentType">Which type view is this? Host, AddIn, or Shared</param>
		/// <param name="mi">Original MI from the contract</param>
		/// <param name="method">CodeMemberMethod for the method on the view</param>
		private void AddParametersToViewMethod(SegmentType componentType, MethodInfo mi, CodeMemberMethod method)
		{
			//Iterate through each parameter on the type in the original contract method
			foreach (ParameterInfo pi in mi.GetParameters())
			{
				CodeTypeReference paramType = GetViewTypeReference(componentType, pi.ParameterType, mi.DeclaringType,
				                                                   SegmentDirection.None);
				var cp = new CodeParameterDeclarationExpression(paramType, pi.Name);
				if (IsOut(pi))
				{
					cp.Direction = FieldDirection.Out;
				}
				else if (IsByRef(pi))
				{
					cp.Direction = FieldDirection.Ref;
				}
				method.Parameters.Add(cp);
			}
		}

		private static bool IsByRef(ParameterInfo pi)
		{
			return (!pi.IsOut && pi.ParameterType.Name.EndsWith("&"));
		}

		private static bool IsOut(ParameterInfo pi)
		{
			return pi.IsOut;
		}


		private Type GetCannonicalContractType(Type contractType)
		{
			if (contractType.FullName.EndsWith("&"))
			{
				String newName = contractType.FullName.Substring(0, contractType.FullName.Length - 1);
				String newOpenName = contractType.Name.Substring(0, contractType.Name.Length - 1);
				if (contractType.GetGenericArguments().Length == 0)
				{
					contractType = contractType.Assembly.GetType(newName);
				}
				else
				{
					if (newOpenName.Equals(typeof (IListContract<>).Name))
					{
						contractType = typeof (IListContract<>).MakeGenericType(contractType.GetGenericArguments());
					}
					else
					{
						throw new InvalidOperationException("Pipeline Builder does not currently support arbitrary generic contracts");
					}
				}
			}
			return contractType;
		}

		//private CodeTypeReference GetViewTypeReference(SegmentType componentType, Type contractType, Type declaringType)
		//{
		//    //If the return value does not need adapting set the return value as the actual type. 
		//    //If it needs adapting but is not an IlistContract set the return value as the view type for the specified return value, 
		//    //otherwise set the return type as IList<TView> for the IListContract<TContract>
		//    contractType = GetCannonicalContractType(contractType);

		//    if (!TypeNeedsAdapting(contractType))
		//    {
		//        return new CodeTypeReference(contractType);
		//    }
		//    else
		//    {
		//        if (IsIListContract(contractType))
		//        {
		//            CodeTypeReference returnType = GetIListContractTypeRef(componentType, SegmentDirection.None, contractType, declaringType);
		//            return returnType;
		//        }
		//        else if (IsNativeHandle(contractType))
		//        {
		//            return GetNativeHandleViewType(SegmentDirection.ViewToContract);
		//        }
		//        else
		//        {
		//            return new CodeTypeReference(_Symbols.GetNameFromType(contractType, componentType, SegmentDirection.None,declaringType));
		//        }


		//    }
		//}

		private CodeTypeReference GetViewTypeReference(SegmentType componentType, Type contractType, Type declaringType,
		                                               SegmentDirection direction)
		{
			//If the return value does not need adapting set the return value as the actual type. 
			//If it needs adapting but is not an IlistContract set the return value as the view type for the specified return value, 
			//otherwise set the return type as IList<TView> for the IListContract<TContract>
			contractType = GetCannonicalContractType(contractType);

			if (!TypeNeedsAdapting(contractType))
				return new CodeTypeReference(contractType);

			if (IsIListContract(contractType))
			{
				CodeTypeReference returnType = GetIListContractTypeRef(componentType, direction, contractType, declaringType);
				return returnType;
			}
			if (IsNativeHandle(contractType))
			{
				return GetNativeHandleViewType(SegmentDirection.ViewToContract);
			}

			return new CodeTypeReference(_Symbols.GetNameFromType(contractType, componentType, direction, declaringType));
		}


		/// <summary>
		/// Build the CodeTypeReference for an Event on the view
		/// </summary>
		/// <param name="componentType">Which type of view: addin, host, shared?</param>
		/// <param name="mi">MethodInfo from the original contract</param>
		/// <param name="fullyQualified">Should we use a namespace prefix for the view type</param>
		/// <returns></returns>
		private CodeTypeReference GetEventViewType(SegmentType componentType, MethodInfo mi, bool forAdapter)
		{
			CodeTypeReference viewArgsType = GetEventArgsType(componentType, mi, forAdapter);
			var eventType = new CodeTypeReference(typeof (EventHandler<>));
			eventType.TypeArguments.Add(viewArgsType);
			eventType.UserData["eventArgsTypeName"] = viewArgsType.UserData["typeName"];
			return eventType;
		}

		/// <summary>
		/// Determines if this type should be expressed as an interface in the view
		/// </summary>
		/// <param name="contractType">Original contract type</param>
		/// <returns></returns>
		internal static bool IsViewInterface(Type contractType)
		{
			return !((contractType.GetCustomAttributes(typeof (AbstractBaseClass), false).Length > 0) ||
			         IsEventArgs(contractType) ||
			         contractType.IsEnum ||
			         contractType.IsValueType ||
			         contractType.IsArray);
		}


		//A type has been specified as an event args type iff it has the event args attribute applied to its contract
		private static bool IsEventArgs(Type contractType)
		{
			return contractType.GetCustomAttributes(typeof (EventArgsAttribute), false).Length > 0;
		}

		private static EventArgsAttribute GetEventArgs(Type contractType)
		{
			try
			{
				return (EventArgsAttribute) contractType.GetCustomAttributes(typeof (EventArgsAttribute), false)[0];
			}
			catch (IndexOutOfRangeException)
			{
				throw new InvalidOperationException(
					"Tried to get the event args attribute from a type that is not marked with that attribute: " +
					contractType.FullName);
			}
		}

		private CodeTypeReference GetIListContractTypeRef(SegmentType componentType, SegmentDirection direction,
		                                                  Type contractGenericType, Type referenceType)
		{
			try
			{
				Type genericParameter = contractGenericType.GetGenericArguments()[0];
				var returnType = new CodeTypeReference(typeof (IList<>));
				returnType.TypeArguments.Add(
					new CodeTypeReference(_Symbols.GetNameFromType(genericParameter, componentType, direction, referenceType)));
				return returnType;
			}
			catch (IndexOutOfRangeException)
			{
				throw new InvalidOperationException("Tried to get the generic arguments for a type that does not have them: " +
				                                    contractGenericType.FullName);
			}
		}

		//Since multiple methods on the contract correspond to one property type on the view (in the case of a property with a getter and a setter we need to keep
		//track of which properties have been created before to decide whether we need to create a new one or add either a gettter or setter to an existing one. 
		//This method looks in a global hashtable for the property it needs and if it doesn't find one it creates one and inserts it into the table. 
		internal CodeMemberProperty GetProperyDecl(Type contractType, CodeTypeDeclaration type, MethodInfo mi,
		                                           Dictionary<String, CodeMemberProperty> props, SegmentType componentType,
		                                           SegmentDirection direction, bool prefix)
		{
			CodeMemberProperty prop;
			if (!props.TryGetValue(GetViewNameFromMethod(mi), out prop))
			{
				PropertyMethodInfo attr = GetPropertyAttribute(mi);
				prop = new CodeMemberProperty();
				prop.Name = GetViewNameFromMethod(mi);

				if (direction == SegmentDirection.None)
				{
					prop.Attributes = MemberAttributes.Abstract | MemberAttributes.Public;
				}
				else if (direction == SegmentDirection.ContractToView)
				{
					if (!IsViewInterface(contractType))
					{
						prop.Attributes = MemberAttributes.Public | MemberAttributes.Override;
					}
					else
					{
						prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
					}
				}
				else
				{
					prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				}
				type.Members.Add(prop);
				props.Add(GetViewNameFromMethod(mi), prop);
				Type propertyType = GetPropertyType(mi);
				if (direction.Equals(SegmentDirection.ViewToContract))
				{
					prop.Type = new CodeTypeReference(propertyType);
				}
				else
				{
					if (IsIListContract(propertyType))
					{
						prop.Type = GetIListContractTypeRef(componentType, direction, propertyType, mi.DeclaringType);
					}
					else if (IsNativeHandle(propertyType))
					{
						prop.Type = GetNativeHandleViewType(SegmentDirection.ViewToContract);
					}
					else
					{
						prop.Type = new CodeTypeReference(_Symbols.GetNameFromType(propertyType, componentType, direction, contractType));
					}
				}
			}
			return prop;
		}

		private Type GetPropertyType(MethodInfo mi)
		{
			PropertyMethodInfo attr = GetPropertyAttribute(mi);
			switch (attr.Type)
			{
				case PropertyType.get:
					return mi.ReturnType;
				case PropertyType.set:
					{
						if (mi.GetParameters().Length != 1)
							throw new InvalidOperationException(
								"Property setter indicated on a method that has more than one input parameter: " + mi.Name + " on " +
								mi.ReflectedType.Name);
						return mi.GetParameters()[0].ParameterType;
					}
				default:
					return null;
			}
		}

		private static bool IsIListContract(Type t)
		{
			return t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition().Equals(typeof (IListContract<>));
		}

		private static Type GetListGenericParamterType(Type t)
		{
			return t.GetGenericArguments().Length == 1 ? t.GetGenericArguments()[0] : null;
		}

		private static bool IsEventAdd(MethodInfo mi)
		{
			return mi.GetCustomAttributes(typeof (EventAddAttribute), false).Length > 0;
		}

		private static bool IsEventRemove(MethodInfo mi)
		{
			return mi.GetCustomAttributes(typeof (EventRemoveAttribute), false).Length > 0;
		}


		private static EventAddAttribute GetEventAdd(MethodInfo mi)
		{
			if (IsEventAdd(mi))
			{
				return (EventAddAttribute) mi.GetCustomAttributes(typeof (EventAddAttribute), false)[0];
			}
			else
			{
				return null;
			}
		}

		private static EventRemoveAttribute GetEventRemove(MethodInfo mi)
		{
			if (IsEventRemove(mi))
			{
				return (EventRemoveAttribute) mi.GetCustomAttributes(typeof (EventRemoveAttribute), false)[0];
			}
			return null;
		}

		internal static PropertyMethodInfo GetPropertyAttribute(MethodInfo mi)
		{
			if (IsProperty(mi))
			{
				if (mi.Name.StartsWith("get_"))
				{
					return new PropertyMethodInfo(PropertyType.get, mi.Name.Substring(4));
				}
				if (mi.Name.StartsWith("set_"))
				{
					return new PropertyMethodInfo(PropertyType.set, mi.Name.Substring(4));
				}
			}

			return null;
		}


		internal static bool IsProperty(MethodInfo mi)
		{
			//setter
			if (mi.Name.StartsWith("set_") &&
			    mi.ReturnType.Equals(typeof (void)) &&
			    mi.GetParameters().Length == 1)
			{
				return true;
			}
			//getter
			return mi.Name.StartsWith("get_") &&
			       !mi.ReturnType.Equals(typeof (void)) &&
			       mi.GetParameters().Length == 0;
		}

		internal static string GetViewNameFromMethod(MethodInfo mi)
		{
			if (IsProperty(mi))
			{
				PropertyMethodInfo prop = GetPropertyAttribute(mi);
				return prop.Name;
			}
			return mi.Name;
		}


		//For a given method, marked as an event, get a type reference for the event args type for the view.
		private CodeTypeReference GetEventArgsType(SegmentType componentType, MethodInfo mi, bool forAdapter)
		{
			//The fist and only parameter should be for contract representing the delegate type
			if (mi.GetParameters().Length != 1)
			{
				throw new InvalidOperationException(
					"A method specified as an event does has the wrong number of parameters (not 1): " + mi.Name + " on " +
					mi.ReflectedType.Name);
			}
			ParameterInfo pi = mi.GetParameters()[0];
			//The contract representing the delegate type should have exactly one method
			if (pi.ParameterType.GetMethods().Length != 1)
			{
				throw new InvalidOperationException(
					"A type specified as an event delegate does has the wrong number of methods (not 1): " + pi.Name);
			}
			MethodInfo eventMethod = pi.ParameterType.GetMethods()[0];
			CodeTypeReference viewArgsType;
			//If the method has exactly one parameter, then that parameters type represents the type of event args to use. 
			//If the method has no parameters default to EventArgs
			//Else there is an error
			if (eventMethod.GetParameters().Length > 0)
			{
				if (eventMethod.GetParameters().Length != 1)
				{
					throw new InvalidOperationException("More than one parameters have been specified for an event delegate: " +
					                                    eventMethod.Name + " on " + eventMethod.ReflectedType.Name);
				}
				pi = eventMethod.GetParameters()[0];
				String typeName;
				if (forAdapter)
				{
					typeName = _Symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.None, true);
				}
				else
				{
					typeName = _Symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.None, mi.DeclaringType);
				}
				viewArgsType = new CodeTypeReference(typeName);
				viewArgsType.UserData["typeName"] = typeName;
			}
			else
			{
				viewArgsType = new CodeTypeReference("EventArgs", CodeTypeReferenceOptions.GlobalReference);
			}

			return viewArgsType;
		}


		/// <summary>
		/// This class builds the static adapters that are called directly from the adapters for other types for adapting of return values and parameters. 
		/// They are responsible for calling the constructors of the appropriate types to create the adapters and for knowing when to adapt a type and when to unwrap
		/// </summary>
		/// <param name="contractType">Type of the contract to be adapted</param>
		/// <param name="component">Component that the source should be placed in. </param>
		/// <param name="componentType">Type of component to be built: either asa or hsa</param>
		internal void BuildStaticAdapters(Type contractType, PipelineSegmentSource component, SegmentType componentType)
		{
			if (contractType.GetCustomAttributes(typeof (EventHandlerAttribute), false).Length > 0)
			{
				return;
			}
			String typeName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, false);
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(componentType, contractType));
			var type = new CodeTypeDeclaration(typeName);
			type.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
			SegmentType viewComponentType;
			if (componentType == SegmentType.AddInSideAdapter)
			{
				viewComponentType = SegmentType.AddInView;
			}
			else if (componentType == SegmentType.HostSideAdapter)
			{
				viewComponentType = SegmentType.HostAddInView;
			}
			else
			{
				throw new InvalidOperationException("Wrong component type");
			}
			var viewType =
				new CodeTypeReference(_Symbols.GetNameFromType(contractType, viewComponentType, SegmentDirection.None, true));
			//Contract to view adapter
			CodeMemberMethod cva;
			CodeMemberMethod vca;
			cva = CreateContractToViewStaticAdapter(contractType, componentType, viewType);
			vca = CreateViewToContractStaticAdapter(contractType, componentType, viewType);
			type.Members.Add(cva);
			type.Members.Add(vca);
			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(typeName, ccu));
		}


		private CodeMemberMethod CreateContractToViewStaticAdapter(Type contractType, SegmentType componentType,
		                                                           CodeTypeReference viewType)
		{
			var cva = new CodeMemberMethod();
			cva.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
			var adapterType =
				new CodeTypeReference(_Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ContractToView, false));
			cva.Name = _Symbols.GetStaticAdapterMethodNameName(contractType, componentType, SegmentDirection.ContractToView);
			cva.ReturnType = viewType;
			var contractParam = new CodeParameterDeclarationExpression(contractType, "contract");
			cva.Parameters.Add(contractParam);


			if (contractType.IsArray)
			{
				return CreateArrayContractToViewStaticAdapter(contractType, componentType, viewType);
			}
			else if (contractType.IsEnum)
			{
				var cce = new CodeCastExpression(viewType, new CodeVariableReferenceExpression("contract"));
				cva.Statements.Add(new CodeMethodReturnStatement(cce));
			}
			else if (contractType.IsValueType)
			{
				return CreateStructContractToViewStaticAdapter(contractType, componentType, viewType);
			}
			else
			{
				//Check for null contract and return null instead of adapting
				var contract = new CodeVariableReferenceExpression("contract");
				var nullContractCheck = new CodeConditionStatement();
				nullContractCheck.Condition = new CodeBinaryOperatorExpression(contract, CodeBinaryOperatorType.IdentityEquality,
				                                                               new CodePrimitiveExpression(null));
				nullContractCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
				cva.Statements.Add(nullContractCheck);

				#region TryUpCast

				List<Type> subTypes;
				if (_TypeHierarchy.TryGetValue(contractType, out subTypes))
				{
					cva.Statements.Add(new CodeVariableDeclarationStatement(typeof (IContract), "subContract"));
					var subContractRef = new CodeVariableReferenceExpression("subContract");
					foreach (Type t in subTypes)
					{
						var assign = new CodeAssignStatement();
						var queryContract =
							new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("contract"),
							                               "QueryContract",
							                               new CodePrimitiveExpression(t.AssemblyQualifiedName));
						assign.Left = subContractRef;
						assign.Right = queryContract;
						var ifContractFound = new CodeConditionStatement();
						var nullCheck =
							new CodeBinaryOperatorExpression(subContractRef,
							                                 CodeBinaryOperatorType.IdentityInequality,
							                                 new CodePrimitiveExpression(null));
						ifContractFound.Condition = nullCheck;
						var castContract = new CodeCastExpression(t, subContractRef);
						CodeMethodInvokeExpression subTypeAdapter = CallStaticAdapter(componentType, t, castContract,
						                                                              SegmentDirection.ContractToView);
						ifContractFound.TrueStatements.Add(new CodeMethodReturnStatement(subTypeAdapter));
						cva.Statements.Add(assign);
						cva.Statements.Add(ifContractFound);
					}
				}

				#endregion

				String outgoingAdapterName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ViewToContract,
				                                                      false);
				var tryCast = new CodeConditionStatement();
				//Create a new ViewToContractAdapter and pass out
				var adapterNew = new CodeObjectCreateExpression(adapterType, contract);
				tryCast.FalseStatements.Add(new CodeMethodReturnStatement(adapterNew));
				//Cast to our ViewToContractAdapter and return original view
				var cast = new CodeCastExpression(outgoingAdapterName, contract);
				var getContract = new CodeMethodInvokeExpression(cast, "GetSourceView");
				tryCast.TrueStatements.Add(new CodeMethodReturnStatement(getContract));
				//Create the type check
				var getType = new CodeMethodInvokeExpression(contract, "GetType");
				var equals = new CodeMethodInvokeExpression(getType, "Equals");
				var typeofExpr = new CodeTypeOfExpression(outgoingAdapterName);
				equals.Parameters.Add(typeofExpr);
				//Check to see if it's a local object
				var isRemote =
					new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof (RemotingServices)), "IsObjectOutOfAppDomain",
					                               contract);
				var canUnwrap = new CodeBinaryOperatorExpression();
				canUnwrap.Operator = CodeBinaryOperatorType.BooleanAnd;
				canUnwrap.Right = equals;
				canUnwrap.Left = new CodeBinaryOperatorExpression(isRemote, CodeBinaryOperatorType.IdentityInequality,
				                                                  new CodePrimitiveExpression(true));

				tryCast.Condition = canUnwrap;

				cva.Statements.Add(tryCast);
			}

			return cva;
		}

		private CodeMemberMethod CreateViewToContractStaticAdapter(Type contractType, SegmentType componentType,
		                                                           CodeTypeReference viewType)
		{
			var vca = new CodeMemberMethod();
			vca.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
			var adapterType =
				new CodeTypeReference(_Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ViewToContract, false));
			vca.Name = _Symbols.GetStaticAdapterMethodNameName(contractType, componentType, SegmentDirection.ViewToContract);
			vca.ReturnType = new CodeTypeReference(contractType);
			var viewParam = new CodeParameterDeclarationExpression(viewType, "view");
			vca.Parameters.Add(viewParam);


			if (contractType.IsArray)
			{
				return CreateArrayViewToContractStaticAdapter(contractType, componentType, viewType);
			}

			if (contractType.IsEnum)
			{
				var cce = new CodeCastExpression(contractType, new CodeVariableReferenceExpression("view"));
				vca.Statements.Add(new CodeMethodReturnStatement(cce));
			}
			else if (contractType.IsValueType)
			{
				return CreateStructViewtoContractStaticAdapter(contractType, componentType, viewType);
			}
			else
			{
				//Check for null contract and return null instead of adapting
				var view = new CodeVariableReferenceExpression("view");
				var nullContractCheck = new CodeConditionStatement();
				nullContractCheck.Condition = new CodeBinaryOperatorExpression(view, CodeBinaryOperatorType.IdentityEquality,
				                                                               new CodePrimitiveExpression(null));
				nullContractCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
				vca.Statements.Add(nullContractCheck);

				var subTypes = new List<Type>();
				_TypeHierarchy.TryGetValue(contractType, out subTypes);
				if (subTypes != null)
				{
					foreach (Type subType in subTypes)
					{
						var upCastCheck = new CodeConditionStatement();
						String viewTypeName = _Symbols.GetNameFromType(subType, GetViewType(componentType), SegmentDirection.None, true);
						var isExpr = new CodeSnippetExpression();
						isExpr.Value = "view is " + viewTypeName;
						upCastCheck.Condition = isExpr;
						upCastCheck.TrueStatements.Add(
							new CodeMethodReturnStatement(
								CallStaticAdapter(componentType,
								                  subType,
								                  new CodeCastExpression(viewTypeName, new CodeVariableReferenceExpression("view")),
								                  SegmentDirection.ViewToContract)));
						vca.Statements.Add(upCastCheck);
					}
				}

				String incomingAdapterName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ContractToView,
				                                                      false);
				var tryCast = new CodeConditionStatement();
				//Create a new ViewToContractAdapter and pass out
				var adapterNew = new CodeObjectCreateExpression(adapterType, view);
				tryCast.FalseStatements.Add(new CodeMethodReturnStatement(adapterNew));
				//Cast to our ContractToViewAdapter and return original contract
				var cast = new CodeCastExpression(incomingAdapterName, view);
				var getContract = new CodeMethodInvokeExpression(cast, "GetSourceContract");
				tryCast.TrueStatements.Add(new CodeMethodReturnStatement(getContract));
				//Create the type check
				var getType = new CodeMethodInvokeExpression(view, "GetType");
				var equals = new CodeMethodInvokeExpression(getType, "Equals");
				var typeofExpr = new CodeTypeOfExpression(incomingAdapterName);
				equals.Parameters.Add(typeofExpr);
				tryCast.Condition = equals;
				vca.Statements.Add(tryCast);
			}
			return vca;
		}

		internal void BuildViewToContractAdapter(Type contractType, PipelineSegmentSource component, SegmentType componentType,
		                                         bool activatable)
		{
			//Set up type
			String typeName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ViewToContract, false);
			var props = new Dictionary<string, CodeMemberProperty>();
			SegmentType viewType;
			if (componentType == SegmentType.AddInSideAdapter)
			{
				viewType = SegmentType.AddInView;
			}
			else
			{
				viewType = SegmentType.HostAddInView;
			}
			String viewName = _Symbols.GetNameFromType(contractType, viewType);
			//If this is an event type determine which real type this is an event on
			if (contractType.GetCustomAttributes(typeof (EventHandlerAttribute), false).Length > 0)
			{
				viewName = "System.Object";
			}
			//Set up the namespace and the type declaration
			//Derive from contractbase and the specific contract
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(componentType, contractType));
			var type = new CodeTypeDeclaration(typeName);
			type.TypeAttributes = TypeAttributes.Public;
			type.BaseTypes.Add(new CodeTypeReference(typeof (ContractBase)));
			type.BaseTypes.Add(new CodeTypeReference(contractType));
			//If this is activatable mark it with the addinadapterattribute
			//The viewtocontract adapter is only ever activatable in the add-in side adapter so no need to check which adapter we're in
			if (activatable)
			{
				var marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof (AddInAdapterAttribute)));
				type.CustomAttributes.Add(marker);
			}
			var aib = new CodeMemberField(viewName, "_view");
			type.Members.Add(aib);
			//Build constructor
			//Add parameter for view type and assign it to member field _view
			var constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public;
			var parameter = new CodeParameterDeclarationExpression(viewName, "view");
			constructor.Parameters.Add(parameter);
			var assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_view"),
			                                     new CodeVariableReferenceExpression("view"));
			constructor.Statements.Add(assign);
			type.Members.Add(constructor);
			if (IsEvent(contractType))
			{
				//If this is an event type we have an additional constructor paramter that is the fieldinfo object for the eventhandler we need to invoke. 
				//Add this parameter to the constructor and then store it in a member variable.
				var eventMember = new CodeMemberField(typeof (MethodInfo), "_event");
				eventMember.Attributes |= MemberAttributes.Private;
				type.Members.Add(eventMember);
				constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof (MethodInfo), "eventProp"));
				constructor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("_event"),
				                                                   new CodeVariableReferenceExpression("eventProp")));
				//We have already validated that this is an event so the attribute exists
				//We have also validated, while creating the views, that this contract has one method and that one method takes in one parameter
				//The one method on this type acts as the delegate with its parameter representing the event args.
				MethodInfo mi = contractType.GetMethods()[0];
				CodeExpression args = null;
				CodeTypeReference parameterType;
				ParameterInfo pi = mi.GetParameters()[0];
				parameterType = new CodeTypeReference(pi.ParameterType);

				var method = new CodeMemberMethod();
				method.Name = mi.Name;
				method.Parameters.Add(new CodeParameterDeclarationExpression(parameterType, "args"));
				method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

				var adaptArgs = new CodeStatementCollection();
				//If the parameter type needs to be adapted to pass it to the contract then new up an adapter and pass that along as a parameter
				//Else simply pass the args in directly as the parameter. 
				CodeTypeReference eventArgsViewType;
				if (TypeNeedsAdapting(pi.ParameterType))
				{
					var adaptedArgs = new CodeObjectCreateExpression();
					adaptedArgs.Parameters.Add(new CodeVariableReferenceExpression("args"));
					adaptedArgs.CreateType =
						new CodeTypeReference(_Symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ContractToView,
						                                               contractType));
					adaptedArgs.CreateType = GetViewTypeReference(viewType, pi.ParameterType, contractType,
					                                              SegmentDirection.ContractToView);
					var adapterArgsDeclare = new CodeVariableDeclarationStatement(adaptedArgs.CreateType, "adaptedArgs");
					var assignArgs = new CodeAssignStatement(new CodeVariableReferenceExpression("adaptedArgs"), adaptedArgs);
					assignArgs.Right = CallStaticAdapter(componentType, pi.ParameterType, new CodeTypeReferenceExpression("args"),
					                                     SegmentDirection.ContractToView);
					adaptArgs.Add(adapterArgsDeclare);
					adaptArgs.Add(assignArgs);
					args = new CodeVariableReferenceExpression("adaptedArgs");
					eventArgsViewType = new CodeTypeReference(_Symbols.GetNameFromType(pi.ParameterType, viewType));
				}
				else
				{
					args = new CodeVariableReferenceExpression("args");
					eventArgsViewType = new CodeTypeReference(pi.ParameterType);
				}


				method.Statements.AddRange(adaptArgs);
				var argsArray =
					new CodeVariableDeclarationStatement(new CodeTypeReference(typeof (object[])), "argsArray");
				argsArray.InitExpression =
					new CodeArrayCreateExpression(new CodeTypeReference(typeof (object)), new CodePrimitiveExpression(1));
				var addToArgsArray = new CodeAssignStatement();
				addToArgsArray.Left =
					new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("argsArray"), new CodePrimitiveExpression(0));
				addToArgsArray.Right = args;
				var eventInvoke = new CodeMethodInvokeExpression();
				eventInvoke.Method.MethodName = "Invoke";
				eventInvoke.Method.TargetObject = new CodeVariableReferenceExpression("_event");
				eventInvoke.Parameters.Add(new CodeVariableReferenceExpression("_view"));
				eventInvoke.Parameters.Add(new CodeVariableReferenceExpression("argsArray"));
				method.Statements.Add(argsArray);
				method.Statements.Add(addToArgsArray);
				method.Statements.Add(eventInvoke);


				//This is a cancelable event args
				//Return args.Cancel if event is hooked up
				//If event is not hooked up return false (don't cancel)
				if (mi.ReturnType.Equals(typeof (bool)))
				{
					if (!GetEventArgs(pi.ParameterType).Cancelable)
					{
						throw new InvalidOperationException("Event handler method returns a bool but the event args are not cancelable:" +
						                                    mi.DeclaringType.FullName + "." + mi.Name);
					}
					method.ReturnType = new CodeTypeReference(typeof (bool));
					var cancel = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("adaptedArgs"), "Cancel");
					var retCancelCheck = new CodeMethodReturnStatement(cancel);
					method.Statements.Add(retCancelCheck);
				}

				type.Members.Add(method);
			}
			else // non-event
			{
				//For standard contract types we simply iterate through each method and build an adapter one by one
				foreach (MethodInfo mi in GetMethodsFromContract(contractType, true))
				{
					var method = new CodeMemberMethod();
					var cmi = new CodeMethodInvokeExpression();
					var prologue = new CodeStatementCollection();
					var epilogue = new CodeStatementCollection();
					var ret = new CodeMethodReturnStatement();
					cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_view"), mi.Name);
					method.Attributes = MemberAttributes.Public;
					method.Name = mi.Name;
					//Set the methods return type appropriately
					if (mi.ReturnType.Equals(typeof (void)))
					{
						//If return type is void return type is null and there is no return statement. 
						method.ReturnType = new CodeTypeReference(mi.ReturnType);
						ret = null;
					}
					else if (!TypeNeedsAdapting(mi.ReturnType))
					{
						//If the return type does not need adapting return type mirrors the contract type and the return value is the direct result of the method call
						method.ReturnType = new CodeTypeReference(mi.ReturnType);
						ret.Expression = cmi;
					}
					else
					{
						method.ReturnType = new CodeTypeReference(mi.ReturnType);

						//If the return type needs adapting call method, pass its return value to the static adapter, and then return that
						CodeMethodInvokeExpression adaptExpr = CallStaticAdapter(componentType, mi.ReturnType, cmi,
						                                                         SegmentDirection.ViewToContract);
						ret.Expression = adaptExpr;
					}
					if (IsProperty(mi))
					{
						method = null;

						CodeMemberProperty myProp;

						bool prefix = false;
						myProp = GetProperyDecl(contractType, type, mi, props, componentType, SegmentDirection.ViewToContract, prefix);
						CodeStatement statement = GetPropertyViewToContractAdapterImpl(componentType, viewType, mi, null);
						switch (GetPropertyAttribute(mi).Type)
						{
							case PropertyType.set:
								myProp.HasSet = true;
								myProp.SetStatements.Add(statement);
								break;
							case PropertyType.get:
								myProp.HasGet = true;
								myProp.GetStatements.Add(statement);
								break;
						}

						//If this method needs to be expressed as a property in the views we need to treat it differently. 
					}
					else if (IsEventAdd(mi))
					{
						//This indicates that the method we are in really represents an event handler on the view type rather than a method
						//We need to hook this up such that firing this event on one side causes it to fire on the other side
						//We've already validated that these are valid setevents when building the views
						EventAddAttribute attr = GetEventAdd(mi);
						CodeTypeReference eventArgsType = GetEventArgsType(viewType, mi, true);
						ParameterInfo pi = mi.GetParameters()[0];
						//Build a reference to the event handler on the view object
						var eventHandler =
							new CodeEventReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
						//Get the adapter for the event args type
						var adapterType =
							new CodeTypeReference(_Symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ContractToView,
							                                               contractType));
						var adapterConstruct = new CodeObjectCreateExpression(adapterType, new CodeVariableReferenceExpression(pi.Name));
						var handler = new CodeDelegateCreateExpression();
						//Build the event handler
						handler.DelegateType = new CodeTypeReference(typeof (EventHandler<>));
						handler.DelegateType.TypeArguments.Add(eventArgsType);
						handler.TargetObject = adapterConstruct;
						handler.MethodName = "Handler";
						var handlerVar = new CodeVariableDeclarationStatement();
						handlerVar.Type = handler.DelegateType;
						handlerVar.Name = "adaptedHandler";
						handlerVar.InitExpression = handler;
						method.Statements.Add(handlerVar);
						//Add attach the handler to the eventhandler type on the view and finish building the method
						var attach = new CodeAttachEventStatement(eventHandler, new CodeVariableReferenceExpression(handlerVar.Name));
						var cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
						method.Parameters.Add(cp);
						method.Statements.Add(attach);

						//Add Dictionary of adapters
						var dictionaryType = new CodeTypeReference("System.Collections.Generic.Dictionary");
						dictionaryType.TypeArguments.Add(new CodeTypeReference(pi.ParameterType));
						dictionaryType.TypeArguments.Add(handler.DelegateType);
						var handlers = new CodeMemberField();
						handlers.Name = attr.Name + "_handlers";
						handlers.Type = dictionaryType;
						type.Members.Add(handlers);
						//Initialize Dictionary of Adapters;
						var createDictionary = new CodeObjectCreateExpression();
						createDictionary.CreateType = dictionaryType;
						var initDictionary = new CodeAssignStatement();
						initDictionary.Left = new CodeVariableReferenceExpression(handlers.Name);
						initDictionary.Right = createDictionary;
						constructor.Statements.Add(initDictionary);
						//Add current handler to the dictionary
						var storeHandler = new CodeAssignStatement();
						var dictLocation = new CodeArrayIndexerExpression();
						dictLocation.TargetObject = new CodeVariableReferenceExpression(handlers.Name);
						dictLocation.Indices.Add(new CodeVariableReferenceExpression(pi.Name));
						storeHandler.Left = dictLocation;
						storeHandler.Right = new CodeVariableReferenceExpression(handlerVar.Name);
						method.Statements.Add(storeHandler);
					}
					else if (IsEventRemove(mi))
					{
						EventRemoveAttribute attr = GetEventRemove(mi);
						ParameterInfo pi = mi.GetParameters()[0];
						//Declare the handler 

						CodeTypeReference eventArgsType = GetEventArgsType(viewType, mi, true);
						var handlerVar = new CodeVariableDeclarationStatement();
						handlerVar.Name = "adaptedHandler";
						handlerVar.Type = new CodeTypeReference(typeof (EventHandler<>));
						handlerVar.Type.TypeArguments.Add(eventArgsType);
						method.Statements.Add(handlerVar);
						//TryGet handler from handlers
						var tryGet = new CodeMethodInvokeExpression();
						tryGet.Method.TargetObject = new CodeVariableReferenceExpression(attr.Name + "_handlers");
						tryGet.Method.MethodName = "TryGetValue";
						tryGet.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
						tryGet.Parameters.Add(new CodeDirectionExpression(FieldDirection.Out,
						                                                  new CodeVariableReferenceExpression(handlerVar.Name)));

						var ifGotValue = new CodeConditionStatement();
						ifGotValue.Condition = tryGet;
						//Remove handler
						var removeHandler = new CodeMethodInvokeExpression();
						removeHandler.Method.MethodName = "Remove";
						removeHandler.Method.TargetObject = new CodeVariableReferenceExpression(attr.Name + "_handlers");
						removeHandler.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
						ifGotValue.TrueStatements.Add(removeHandler);
						var detach = new CodeRemoveEventStatement();
						detach.Event = new CodeEventReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
						detach.Listener = new CodeVariableReferenceExpression(handlerVar.Name);
						ifGotValue.TrueStatements.Add(detach);
						method.Statements.Add(ifGotValue);
						//Add parameters to method decl
						var cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
						method.Parameters.Add(cp);
					}
					else
					{
						//This is a standard method to adapt, go through each parameter, check to see if it needs adapting. 
						//If no adapting is needed just pass on through, else find the right adapter and use it
						foreach (ParameterInfo pi in mi.GetParameters())
						{
							CodeTypeReference paramViewType = GetViewTypeReference(viewType, pi.ParameterType, contractType,
							                                                       SegmentDirection.ViewToContract);
							Type paramContractType = GetCannonicalContractType(pi.ParameterType);
							if (!TypeNeedsAdapting(paramContractType))
							{
								var cp = new CodeParameterDeclarationExpression(paramContractType, pi.Name);
								CodeExpression param;
								if (IsByRef(pi))
								{
									cp.Direction = FieldDirection.Ref;
									param = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression(pi.Name));
								}
								else if (IsOut(pi))
								{
									cp.Direction = FieldDirection.Out;
									param = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(pi.Name));
								}
								else
								{
									param = new CodeVariableReferenceExpression(pi.Name);
								}
								method.Parameters.Add(cp);
								cmi.Parameters.Add(param);
							}
							else
							{
								var cp = new CodeParameterDeclarationExpression(paramContractType, pi.Name);
								CodeMethodInvokeExpression adapterExpr =
									CallStaticAdapter(componentType, paramContractType, new CodeVariableReferenceExpression(pi.Name),
									                  SegmentDirection.ContractToView);
								if (IsByRef(pi))
								{
									cp.Direction = FieldDirection.Ref;
								}
								if (IsOut(pi))
								{
									cp.Direction = FieldDirection.Out;
								}
								method.Parameters.Add(cp);
								if (!IsByRef(pi) && !IsOut(pi))
								{
									cmi.Parameters.Add(adapterExpr);
								}
								else
								{
									var var = new CodeVariableDeclarationStatement(paramViewType, pi.Name + "_view");
									prologue.Add(var);
									CodeDirectionExpression varRef;
									if (IsByRef(pi))
									{
										var.InitExpression = adapterExpr;
										varRef = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression(var.Name));
									}
									else
									{
										var.InitExpression = new CodeDefaultValueExpression(var.Type);
										varRef = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(var.Name));
									}
									var refVar = new CodeAssignStatement(
										new CodeVariableReferenceExpression(pi.Name),
										CallStaticAdapter(componentType, paramContractType, new CodeVariableReferenceExpression(var.Name),
										                  SegmentDirection.ViewToContract));
									cmi.Parameters.Add(varRef);
									epilogue.Add(refVar);
								}
							}
						}
						if (ret != null)
						{
							//If the previously computed return statement is not null add it to the method 
							//It already has the call to cmi's invocation so no need to add cmi again
							method.Statements.AddRange(prologue);
							if (epilogue.Count > 0)
							{
								var retVar = new CodeVariableDeclarationStatement(mi.ReturnType, "return_variable");
								retVar.InitExpression = ret.Expression;
								ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression(retVar.Name));
								method.Statements.Add(retVar);
								method.Statements.AddRange(epilogue);
								method.Statements.Add(ret);
							}
							else
							{
								method.Statements.Add(ret);
							}
						}
						else
						{
							//If the previously computed return statement is null then add cmi directly to the method statements
							method.Statements.AddRange(prologue);
							method.Statements.Add(cmi);
							method.Statements.AddRange(epilogue);
						}
					}
					if (method != null)
					{
						type.Members.Add(method);
					}
				}
			}

			//Add the method to unwrap the original view
			var unadapt = new CodeMemberMethod();
			unadapt.Name = "GetSourceView";
			unadapt.ReturnType = new CodeTypeReference(viewName);
			unadapt.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
			unadapt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("_view")));
			type.Members.Add(unadapt);


			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(typeName, ccu));
		}

		private CodeStatement GetPropertyViewToContractAdapterImpl(SegmentType componentType, SegmentType viewType,
		                                                           MethodInfo mi, CodeMemberMethod method)
		{
			PropertyMethodInfo attr = GetPropertyAttribute(mi);
			var prop =
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
			if (attr.Type == PropertyType.get)
			{
				var ret = new CodeMethodReturnStatement();
				if (!TypeNeedsAdapting(mi.ReturnType))
				{
					//If the type does not need adapting just pass the return value directly
					ret.Expression = prop;
				}
				else
				{
					ret.Expression = CallStaticAdapter(componentType, mi.ReturnType, prop, SegmentDirection.ViewToContract);
				}

				return ret;
			}
			else
			{
				var set = new CodeAssignStatement();
				set.Left = prop;
				ParameterInfo pi = mi.GetParameters()[0];
				if (method != null)
				{
					method.Parameters.Add(new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name));
				}
				if (!TypeNeedsAdapting(pi.ParameterType))
				{
					//If the type doesn't need adapting just asign it directly
					set.Right = new CodePropertySetValueReferenceExpression();
				}
				else
				{
					//If this is a standard custom contract call the appropriate static adapter
					set.Right = CallStaticAdapter(componentType, pi.ParameterType, new CodePropertySetValueReferenceExpression(),
					                              SegmentDirection.ContractToView);
				}
				return set;
			}
		}

		private CodeStatement GetPropertyContractToViewAdapterImpl(SegmentType componentType, SegmentType viewType,
		                                                           MethodInfo mi, CodeMemberMethod method)
		{
			PropertyMethodInfo attr = GetPropertyAttribute(mi);
			CodeExpression prop = null;
			if (IsProperty(mi))
			{
				prop = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("_contract"), attr.Name);
			}
			else
			{
				prop = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_contract"), mi.Name);
			}

			if (attr.Type == PropertyType.get)
			{
				var ret = new CodeMethodReturnStatement();
				if (!TypeNeedsAdapting(mi.ReturnType))
				{
					//If the type does not need adapting just pass the return value directly
					ret.Expression = prop;
				}
				else
				{
					//If this is a standard custom contract call the appropriate static adapter
					ret.Expression = CallStaticAdapter(componentType, mi.ReturnType, prop, SegmentDirection.ContractToView);
				}

				return ret;
			}
			else
			{
				CodeExpression value = null;
				ParameterInfo pi = mi.GetParameters()[0];
				if (method != null)
				{
					method.Parameters.Add(new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name));
				}
				if (!TypeNeedsAdapting(pi.ParameterType))
				{
					//If the type doesn't need adapting just asign it directly
					value = new CodePropertySetValueReferenceExpression();
				}
				else
				{
					//If this is a standard custom contract call the appropriate static adapter
					value = CallStaticAdapter(componentType, pi.ParameterType, new CodePropertySetValueReferenceExpression(),
					                          SegmentDirection.ViewToContract);
				}
				if (IsProperty(mi))
				{
					var set = new CodeAssignStatement();
					set.Left = prop;
					set.Right = value;
					return set;
				}
				else
				{
					var set = (CodeMethodInvokeExpression) prop;
					set.Parameters.Add(value);
					return new CodeExpressionStatement(set);
				}
			}
		}


		private static bool IsEvent(Type contractType)
		{
			return contractType.GetCustomAttributes(typeof (EventHandlerAttribute), false).Length > 0;
		}


		private CodeMethodInvokeExpression CallListAdapter(SegmentDirection direction, SegmentType componentType,
		                                                   SegmentType viewType, CodeExpression source, Type genericParamType)
		{
			String genericParamViewName = _Symbols.GetNameFromType(genericParamType, viewType, SegmentDirection.None, true);
			CodeMethodReferenceExpression ContractToViewAdapter = GetStaticAdapter(componentType, genericParamType,
			                                                                       SegmentDirection.ContractToView);
			CodeMethodReferenceExpression ViewToContractAdapter = GetStaticAdapter(componentType, genericParamType,
			                                                                       SegmentDirection.ViewToContract);
			var adapterExpr = new CodeMethodInvokeExpression();
			adapterExpr.Method = new CodeMethodReferenceExpression();
			adapterExpr.Method.TargetObject = new CodeTypeReferenceExpression(typeof (CollectionAdapters));


			adapterExpr.Parameters.Add(source);

			if (direction == SegmentDirection.ViewToContract)
			{
				if (TypeNeedsAdapting(genericParamType))
				{
					adapterExpr.Method.TypeArguments.Add(genericParamViewName);
					adapterExpr.Method.TypeArguments.Add(genericParamType);
					adapterExpr.Parameters.Add(ViewToContractAdapter);
					adapterExpr.Parameters.Add(ContractToViewAdapter);
				}
				else
				{
					adapterExpr.Method.TypeArguments.Add(genericParamType);
				}
				adapterExpr.Method.MethodName = "ToIListContract";
			}
			else
			{
				if (TypeNeedsAdapting(genericParamType))
				{
					adapterExpr.Method.TypeArguments.Add(genericParamType);
					adapterExpr.Method.TypeArguments.Add(genericParamViewName);
					adapterExpr.Parameters.Add(ContractToViewAdapter);
					adapterExpr.Parameters.Add(ViewToContractAdapter);
				}
				else
				{
					adapterExpr.Method.TypeArguments.Add(genericParamType);
				}
				adapterExpr.Method.MethodName = "ToIList";
			}

			return adapterExpr;
		}

		internal bool TypeNeedsAdapting(Type contractType)
		{
			contractType = GetCannonicalContractType(contractType);
			if (!contractType.IsArray)
			{
				return contractType.Assembly.Equals(_ContractAsm) || typeof (IContract).IsAssignableFrom(contractType);
			}
			else
			{
				if (!contractType.GetElementType().IsValueType)
				{
					throw new InvalidOperationException(
						"Only arrays of value types are supported. For collections of reference types (types deriving from IContract, use IListContract<T> instead");
				}
				return TypeNeedsAdapting(contractType.GetElementType());
			}
		}

		internal void BuildContractToViewAdapter(Type contractType, PipelineSegmentSource component, SegmentType componentType,
		                                         bool activatable)
		{
			//Set up type
			String typeName = _Symbols.GetNameFromType(contractType, componentType, SegmentDirection.ContractToView, false);
			SegmentType viewType;
			if (componentType == SegmentType.AddInSideAdapter)
			{
				viewType = SegmentType.AddInView;
			}
			else
			{
				viewType = SegmentType.HostAddInView;
			}
			String viewName = _Symbols.GetNameFromType(contractType, viewType);
			var ccu = new CodeCompileUnit();
			var codeNamespace = new CodeNamespace(_Symbols.GetNameSpace(componentType, contractType));
			var type = new CodeTypeDeclaration(typeName);
			type.TypeAttributes = TypeAttributes.Public;
			type.BaseTypes.Add(new CodeTypeReference(viewName));
			if (activatable)
			{
				var marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof (HostAdapterAttribute)));
				type.CustomAttributes.Add(marker);
			}
			var contract = new CodeMemberField(contractType, "_contract");
			var handle = new CodeMemberField(typeof (ContractHandle), "_handle");
			//AddDisposePattern(type, "_handle", "_contract");
			type.Members.Add(contract);
			type.Members.Add(handle);
			//Build constructor
			var constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public;
			var parameter = new CodeParameterDeclarationExpression(contractType, "contract");
			constructor.Parameters.Add(parameter);
			var assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_contract"),
			                                     new CodeVariableReferenceExpression("contract"));
			constructor.Statements.Add(assign);
			var createHandle = new CodeObjectCreateExpression(typeof (ContractHandle),
			                                                  new CodeVariableReferenceExpression("contract"));
			assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_handle"), createHandle);
			constructor.Statements.Add(assign);
			type.Members.Add(constructor);
			SegmentType viewComponentType;
			switch (componentType)
			{
				case SegmentType.AddInSideAdapter:
					viewComponentType = SegmentType.AddInView;
					break;
				case SegmentType.HostSideAdapter:
					viewComponentType = SegmentType.HostAddInView;
					break;
				default:
					throw new InvalidOperationException("Must be asa or hsa");
			}
			if (IsEvent(contractType))
			{
				var handler = new CodeMemberMethod();
				handler.Name = "Handler";
				handler.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				var sender = new CodeParameterDeclarationExpression(typeof (Object), "sender");
				MethodInfo mi = contractType.GetMethods()[0];

				ParameterInfo pi = mi.GetParameters()[0];
				var eventArgsType =
					new CodeTypeReference(_Symbols.GetNameFromType(pi.ParameterType, viewType, SegmentDirection.None, true));
				var args = new CodeParameterDeclarationExpression(eventArgsType, "args");
				handler.Parameters.Add(sender);
				handler.Parameters.Add(args);
				handler.ReturnType = new CodeTypeReference(typeof (void));
				var cmi = new CodeMethodInvokeExpression();
				if (TypeNeedsAdapting(pi.ParameterType))
				{
					CodeMethodInvokeExpression argsAdapter = CallStaticAdapter(componentType, pi.ParameterType,
					                                                           new CodeVariableReferenceExpression("args"),
					                                                           SegmentDirection.ViewToContract);
					cmi.Parameters.Add(argsAdapter);
				}
				else
				{
					cmi.Parameters.Add(new CodeVariableReferenceExpression("args"));
				}
				cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_contract"), mi.Name);
				if (mi.ReturnType.Equals(typeof (bool)))
				{
					var ifStatement = new CodeConditionStatement();
					var assignCancel = new CodeAssignStatement();
					assignCancel.Left = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("args"), "Cancel");
					assignCancel.Right = new CodePrimitiveExpression(true);
					ifStatement.Condition = cmi;
					ifStatement.TrueStatements.Add(assignCancel);
					handler.Statements.Add(ifStatement);
				}
				else
				{
					handler.Statements.Add(cmi);
				}
				type.Members.Add(handler);
				type.BaseTypes.Clear();
			}
			else
			{
				var props = new Dictionary<string, CodeMemberProperty>();
				var setEvents = new List<MethodInfo>();
				foreach (MethodInfo mi in GetMethodsFromContract(contractType, true))
				{
					var cmi = new CodeMethodInvokeExpression();
					if (IsEventAdd(mi))
					{
						EventAddAttribute attr = GetEventAdd(mi);
						//Add event to list for Static Constructor Initialization
						setEvents.Add(mi);
						//Hook up event during constructor
						ParameterInfo pi = mi.GetParameters()[0];
						var adapterType =
							new CodeTypeReference(_Symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ViewToContract,
							                                               contractType));
						var adapter = new CodeObjectCreateExpression(adapterType, new CodeThisReferenceExpression(),
						                                             new CodeVariableReferenceExpression("s_" + mi.Name + "Fire"));
						var handlerField = new CodeMemberField();
						handlerField.Name = attr.Name + "_Handler";
						handlerField.Type = adapterType;
						type.Members.Add(handlerField);
						var assignHandlerField = new CodeAssignStatement();
						assignHandlerField.Left = new CodeVariableReferenceExpression(handlerField.Name);
						assignHandlerField.Right = adapter;
						constructor.Statements.Add(assignHandlerField);


						//Add field
						var eventField = new CodeMemberEvent();
						eventField.Name = "_" + attr.Name;
						eventField.Type = GetEventViewType(viewComponentType, mi, true);
						type.Members.Add(eventField);

						//Add FireMethod
						var eventFire = new CodeMemberMethod();
						eventFire.Attributes = MemberAttributes.Assembly;
						eventFire.Name = "Fire" + eventField.Name;
						eventFire.Parameters.Add(
							new CodeParameterDeclarationExpression(eventField.Type.TypeArguments[0], "args"));
						var eventFireInvoke = new CodeMethodInvokeExpression();
						eventFireInvoke.Method = new CodeMethodReferenceExpression();
						eventFireInvoke.Method.MethodName = "Invoke";
						eventFireInvoke.Method.TargetObject = new CodeVariableReferenceExpression(eventField.Name);
						eventFireInvoke.Parameters.Add(new CodeThisReferenceExpression());
						eventFireInvoke.Parameters.Add(new CodeVariableReferenceExpression("args"));
						var nullConditionalFire = new CodeConditionStatement();
						var eventNullCheck = new CodeBinaryOperatorExpression();
						eventNullCheck.Left = new CodeVariableReferenceExpression(eventField.Name);
						eventNullCheck.Right = new CodePrimitiveExpression(null);
						eventNullCheck.Operator = CodeBinaryOperatorType.IdentityEquality;
						nullConditionalFire.Condition = eventNullCheck;
						nullConditionalFire.FalseStatements.Add(eventFireInvoke);
						eventFire.Statements.Add(nullConditionalFire);
						type.Members.Add(eventFire);

						CodeSnippetTypeMember snippet = GetEventContractToViewFromSnippet(contractType, mi, eventField);
						//Add override property;
						type.Members.Add(snippet);

						continue;
					}
					else if (IsEventRemove(mi))
					{
						continue;
					}
					var method = new CodeMemberMethod();
					var ret = new CodeMethodReturnStatement();
					cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_contract"), mi.Name);
					method.Attributes = MemberAttributes.Public;
					if (!IsViewInterface(contractType))
					{
						method.Attributes |= MemberAttributes.Override;
					}
					else
					{
						method.Attributes |= MemberAttributes.Final;
					}

					method.Name = mi.Name;

					if (mi.ReturnType.Equals(typeof (void)))
					{
						method.ReturnType = new CodeTypeReference(mi.ReturnType);
						ret = null;
					}
					else if (!TypeNeedsAdapting(mi.ReturnType))
					{
						method.ReturnType = new CodeTypeReference(mi.ReturnType);
						ret.Expression = cmi;
					}
					else
					{
						method.ReturnType = GetViewTypeReference(viewType, mi.ReturnType, mi.DeclaringType,
						                                         SegmentDirection.ContractToView);
						ret.Expression = CallStaticAdapter(componentType, mi.ReturnType, cmi, SegmentDirection.ContractToView);
					}
					var prologue = new CodeStatementCollection();
					var epilogue = new CodeStatementCollection();
					foreach (ParameterInfo pi in mi.GetParameters())
					{
						CodeTypeReference paramType = GetViewTypeReference(viewType, pi.ParameterType, contractType,
						                                                   SegmentDirection.ContractToView);
						if (!TypeNeedsAdapting(pi.ParameterType))
						{
							var cp = new CodeParameterDeclarationExpression(paramType, pi.Name);
							CodeExpression param;
							if (IsByRef(pi))
							{
								cp.Direction = FieldDirection.Ref;
								param = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression(pi.Name));
							}
							else if (IsOut(pi))
							{
								cp.Direction = FieldDirection.Out;
								param = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(pi.Name));
							}
							else
							{
								param = new CodeVariableReferenceExpression(pi.Name);
							}
							method.Parameters.Add(cp);
							cmi.Parameters.Add(param);
						}
						else
						{
							Type paramContractType = GetCannonicalContractType(pi.ParameterType);
							CodeMethodInvokeExpression adaptExpr = CallStaticAdapter(componentType, paramContractType,
							                                                         new CodeVariableReferenceExpression(pi.Name),
							                                                         SegmentDirection.ViewToContract);
							var cp = new CodeParameterDeclarationExpression(paramType, pi.Name);
							if (IsByRef(pi))
							{
								cp.Direction = FieldDirection.Ref;
							}
							if (IsOut(pi))
							{
								cp.Direction = FieldDirection.Out;
							}
							method.Parameters.Add(cp);
							if (!IsByRef(pi) && !IsOut(pi))
							{
								cmi.Parameters.Add(adaptExpr);
							}
							else
							{
								var var = new CodeVariableDeclarationStatement(paramContractType, pi.Name + "_contract");
								prologue.Add(var);
								CodeDirectionExpression varRef;
								if (IsByRef(pi))
								{
									var.InitExpression = adaptExpr;
									varRef = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression(var.Name));
								}
								else
								{
									var.InitExpression = new CodeDefaultValueExpression(var.Type);
									varRef = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(var.Name));
								}
								var refVar = new CodeAssignStatement(
									new CodeVariableReferenceExpression(pi.Name),
									CallStaticAdapter(componentType, paramContractType, new CodeVariableReferenceExpression(var.Name),
									                  SegmentDirection.ContractToView));
								cmi.Parameters.Add(varRef);
								epilogue.Add(refVar);
							}
						}
					}
					if (IsProperty(mi))
					{
						CodeMemberProperty myProp = GetProperyDecl(contractType, type, mi, props, viewComponentType,
						                                           SegmentDirection.ContractToView, true);
						CodeStatement statement = GetPropertyContractToViewAdapterImpl(componentType, viewType, mi, null);
						switch (GetPropertyAttribute(mi).Type)
						{
							case PropertyType.set:
								myProp.HasSet = true;
								myProp.SetStatements.Add(statement);
								break;
							case PropertyType.get:
								myProp.HasGet = true;
								myProp.GetStatements.Add(statement);
								break;
						}
					}
					else
					{
						if (ret != null)
						{
							method.Statements.AddRange(prologue);
							if (epilogue.Count > 0)
							{
								var retVar =
									new CodeVariableDeclarationStatement(
										GetViewTypeReference(viewType, mi.ReturnType, mi.DeclaringType, SegmentDirection.ViewToContract),
										"return_variable");
								retVar.InitExpression = ret.Expression;
								ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression(retVar.Name));
								method.Statements.Add(retVar);
								method.Statements.AddRange(epilogue);
								method.Statements.Add(ret);
							}
							else
							{
								method.Statements.Add(ret);
							}
						}
						else
						{
							method.Statements.AddRange(prologue);
							method.Statements.Add(cmi);
							method.Statements.AddRange(epilogue);
						}

						type.Members.Add(method);
					}
				}
				ProcessSetEvents(viewName, type, setEvents);
			}
			var unadapt = new CodeMemberMethod();
			unadapt.Name = "GetSourceContract";
			unadapt.ReturnType = new CodeTypeReference(contractType);
			unadapt.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
			unadapt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("_contract")));
			type.Members.Add(unadapt);

			codeNamespace.Types.Add(type);
			ccu.Namespaces.Add(codeNamespace);
			component.Files.Add(new SourceFile(typeName, ccu));
		}


		private static CodeSnippetTypeMember GetEventContractToViewFromSnippet(Type t, MethodInfo mi,
		                                                                       CodeMemberEvent eventField)
		{
			EventRemoveAttribute rAttr = null;
			EventAddAttribute attr = GetEventAdd(mi);
			MethodInfo rMi = null;

			foreach (MethodInfo method in t.GetMethods()) // TODO: Verify!!
			{
				if (!IsEventRemove(method) || !GetEventRemove(method).Name.Equals(attr.Name)) continue;

				rAttr = GetEventRemove(method);
				rMi = method;
				break;
			}

			if (rAttr == null)
				throw new InvalidOperationException(string.Format("Can not find matching unsubscribe method for method: {0}.",
				                                                  attr.Name));

			String nl = Environment.NewLine;
			String snippetText = "\t\tpublic event System.EventHandler<{0}>{4}{{" + nl + "\t\t\t";
			snippetText += "add{{" + nl + "\t\t\t\t";
			snippetText += "if ({2} == null)" + nl + "\t\t\t\t";
			snippetText += "{{" + nl + "\t\t\t\t\t";
			snippetText += "_contract.{1}({4}_Handler);" + nl + "\t\t\t\t";
			snippetText += "}}" + nl + "\t\t\t\t";
			snippetText += "{2} += value;" + nl + "\t\t\t\t";
			snippetText += "}}" + nl + "\t\t\t";
			snippetText += "remove{{" + nl + "\t\t\t\t\t";
			snippetText += "{2} -= value;" + nl + "\t\t\t\t";
			snippetText += "if ({2} == null)" + nl + "\t\t\t\t";
			snippetText += "{{" + nl + "\t\t\t\t\t";
			snippetText += "_contract.{3}({4}_Handler);" + nl + "\t\t\t\t";
			snippetText += "}}" + nl + "\t\t\t\t";
			snippetText += "}}" + nl + "\t\t}}";
			snippetText = String.Format(snippetText, eventField.Type.UserData["eventArgsTypeName"], mi.Name, eventField.Name,
			                            rMi.Name, attr.Name);
			var snippet = new CodeSnippetTypeMember(snippetText);
			return snippet;
		}

		private void ProcessSetEvents(String viewName, CodeTypeDeclaration type, List<MethodInfo> events)
		{
			var typeConstructor = new CodeTypeConstructor();
			typeConstructor.Attributes |= MemberAttributes.Private;
			var adapterType = new CodeTypeOfExpression(type.Name);
			foreach (MethodInfo mi in events)
			{
				EventAddAttribute attr = GetEventAdd(mi);
				var field = new CodeMemberField();
				field.Name = "s_" + mi.Name + "Fire";
				field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
				field.Type = new CodeTypeReference(typeof (MethodInfo));
				var init = new CodeAssignStatement();
				var getEventFire = new CodeMethodInvokeExpression();
				getEventFire.Method = new CodeMethodReferenceExpression();
				getEventFire.Method.MethodName = "GetMethod";
				getEventFire.Method.TargetObject = adapterType;
				getEventFire.Parameters.Add(new CodePrimitiveExpression("Fire_" + attr.Name));
				var bindingFlags =
					new CodeCastExpression(new CodeTypeReference(
					                       	typeof (BindingFlags)),
					                       new CodePrimitiveExpression(
					                       	(int) (BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic)));
				getEventFire.Parameters.Add(bindingFlags);
				init.Right = getEventFire;
				init.Left = new CodeVariableReferenceExpression(field.Name);
				typeConstructor.Statements.Add(init);
				type.Members.Add(field);
			}
			type.Members.Add(typeConstructor);
		}

		private void AddDisposePattern(CodeTypeDeclaration type, String handleName, String contractName)
		{
			var disposed = new CodeMemberField(typeof (bool), "_disposed");
			disposed.InitExpression = new CodePrimitiveExpression(false);
			type.BaseTypes.Add(typeof (IDisposable));
			var dispose = new CodeMemberMethod();
			dispose.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			dispose.Name = "Dispose";
			var ifNotDisposed = new CodeConditionStatement();
			ifNotDisposed.Condition = new CodeVariableReferenceExpression("_disposed");
			ifNotDisposed.FalseStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(handleName),
			                                                                 "Dispose"));
			ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(contractName),
			                                                          new CodePrimitiveExpression(null)));
			ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(handleName),
			                                                          new CodePrimitiveExpression(null)));
			ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("_disposed"),
			                                                          new CodePrimitiveExpression(true)));
			ifNotDisposed.FalseStatements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof (GC)),
			                                                                 "SuppressFinalize",
			                                                                 new CodeThisReferenceExpression()));
			dispose.Statements.Add(ifNotDisposed);
			type.Members.Add(dispose);
			type.Members.Add(disposed);
		}

		private CodeMethodInvokeExpression CallStaticAdapter(SegmentType componentType, Type contractType, CodeExpression obj,
		                                                     SegmentDirection direction)
		{
			SegmentType viewType = SegmentType.View;
			if (!ShouldShareViews())
				viewType = componentType.Equals(SegmentType.HostSideAdapter) ? SegmentType.HostAddInView : SegmentType.AddInView;

			if (IsNativeHandle(contractType))
			{
				var adapterInvoke = new CodeMethodInvokeExpression();
				if (direction == SegmentDirection.ContractToView)
				{
					adapterInvoke.Method = new CodeMethodReferenceExpression();
					adapterInvoke.Method.MethodName = "ContractToViewAdapter";
					adapterInvoke.Method.TargetObject =
						new CodeVariableReferenceExpression("System.AddIn.Pipeline.FrameworkElementAdapters");
					adapterInvoke.Parameters.Add(obj);
				}
				else
				{
					adapterInvoke.Method = new CodeMethodReferenceExpression();
					adapterInvoke.Method.MethodName = "ViewToContractAdapter";
					adapterInvoke.Method.TargetObject =
						new CodeVariableReferenceExpression("System.AddIn.Pipeline.FrameworkElementAdapters");
					adapterInvoke.Parameters.Add(obj);
				}
				return adapterInvoke;
			}

			if (IsIListContract(contractType))
			{
				Type genericParam = GetListGenericParamterType(contractType);
				return CallListAdapter(direction, componentType, viewType, obj, genericParam);
			}

			var adaptExpr = new CodeMethodInvokeExpression();
			var adapterType =
				new CodeTypeReferenceExpression(_Symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, true));
			String adapterMethodName = _Symbols.GetStaticAdapterMethodNameName(contractType, componentType, direction);
			var adaptMethod = new CodeMethodReferenceExpression(adapterType, adapterMethodName);
			adaptExpr.Method = adaptMethod;
			adaptExpr.Parameters.Add(obj);
			return adaptExpr;
		}


		private CodeMethodReferenceExpression GetStaticAdapter(SegmentType componentType, Type contractType,
		                                                       SegmentDirection direction)
		{
			var adapterType =
				new CodeTypeReferenceExpression(_Symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, true));
			String adapterMethodName = _Symbols.GetStaticAdapterMethodNameName(contractType, componentType, direction);
			return new CodeMethodReferenceExpression(adapterType, adapterMethodName);
		}

		private static List<MethodInfo> GetMethodsFromContract(Type contract, bool inherit)
		{
			if (!typeof (IContract).IsAssignableFrom(contract))
			{
				throw new InvalidOperationException("Need an IContract type as input");
			}
			var methods = new List<MethodInfo>();
			methods.AddRange(contract.GetMethods());
			foreach (Type t in contract.GetInterfaces())
			{
				if (typeof (IContract).IsAssignableFrom(t) && !t.Equals(typeof (IContract)) && (inherit || !IsBaseType(contract, t)))
				{
					foreach (MethodInfo mi in t.GetMethods())
					{
						if (!methods.Contains(mi))
						{
							methods.Add(mi);
						}
					}
				}
			}
			return methods;
		}

		private static Type GetBaseContract(Type contract)
		{
			foreach (Type t in contract.GetInterfaces())
			{
				if (typeof (IContract).IsAssignableFrom(t) && !t.Equals(typeof (IContract)) && IsBaseType(contract, t))
					return t;
			}
			return null;
		}

		private static bool IsBaseType(Type main, Type sub)
		{
			var attributes = (Attribute[]) main.GetCustomAttributes(typeof (BaseClassAttribute), false);
			foreach (Attribute attr in attributes)
			{
				var baseClass = attr as BaseClassAttribute;
				if (baseClass == null) continue;

				if (baseClass.Base.Equals(sub) || sub.IsAssignableFrom(baseClass.Base))
					return true;
			}
			return false;
		}
	}
}