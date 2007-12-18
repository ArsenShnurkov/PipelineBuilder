/// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.Reflection;
using System.AddIn.Pipeline;
using System.AddIn.Contract;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace PipelineBuilder
{
    public class PipelineBuilder : MarshalByRefObject
    {
        private Assembly _contractAsm;
        private String _asmPath;
        private SymbolTable _symbols;
        private PipelineSegmentSource _hsa;
        private PipelineSegmentSource _aib;
        private PipelineSegmentSource _asa;
        private PipelineSegmentSource _hav;
        private PipelineSegmentSource _view;
        private Dictionary<Type, List<Type>> _typeHierarchy;

        public PipelineBuilder(String assemblyPath)
        {
            _asmPath = assemblyPath;
        }

        public PipelineBuilder(String assemblyPath, bool newDomain)
        {
            Init(assemblyPath, newDomain);
            
        }

       
        public void Init(String assemblyPath, bool newDomain)
        {
            _asmPath = assemblyPath;
            if (!newDomain)
            {
                _contractAsm = Assembly.LoadFrom(assemblyPath);
            }
        }

        internal List<PipelineSegmentSource> BuildRemotePipeline()
        {
            String baseDir = typeof(PipelineBuilder).Assembly.Location;
            baseDir = baseDir.Substring(0,baseDir.LastIndexOf("\\"));
            AppDomain remoteDomain = AppDomain.CreateDomain("WorkerDomain",null,baseDir,null,true);
            List<PipelineSegmentSource> source;
            try
            {
                PipelineBuilder remoteBuilder = (PipelineBuilder)remoteDomain.CreateInstanceAndUnwrap(typeof(PipelineBuilder).Assembly.FullName, typeof(PipelineBuilder).FullName, false, BindingFlags.Public | BindingFlags.Instance, null, new object[] { _asmPath, false }, null, null, null);
               source = remoteBuilder.BuildPipeline();
            }
            finally
            {
                AppDomain.Unload(remoteDomain);
            }
            return source;
        }

        public List<PipelineSegmentSource> BuildPipeline()
        {
            //If we haven't loaded the contract assembly yet it means we want to avoid loading the contract assembly in this domain
            //and should do it remotely. Once we're in the new domain we'll have loaded the contract asm and we'll fall through this
            //and do the work.
            if (_contractAsm == null)
            {               
                List<PipelineSegmentSource> source =  BuildRemotePipeline();
                return source;
            }
            _symbols = new SymbolTable(_contractAsm);
            _typeHierarchy = new Dictionary<Type, List<Type>>();
            List<PipelineSegmentSource> components = new List<PipelineSegmentSource>();
            _aib = new PipelineSegmentSource(SegmentType.AIB, _symbols);
            _asa = new PipelineSegmentSource(SegmentType.ASA, _symbols);
            _hsa = new PipelineSegmentSource(SegmentType.HSA, _symbols);
            _hav = new PipelineSegmentSource(SegmentType.HAV, _symbols);
            _view = new PipelineSegmentSource(SegmentType.VIEW, _symbols);
            components.Add(_asa);
            components.Add(_hsa);
            

            //If the contract assembly is marked as having the views shared we should add a generic "view" component.
            //If not then we build both add-in side and host-side view source files. 
            if (ShouldShareViews())
            {
                components.Add(_view);
            }
            else
            {
                components.Add(_aib);
                components.Add(_hav);
            }
            //Parse the type hierarchy in the contract so we know how to express it in the views. 
            BuildUpCastableTypeHierarchy();
            
            //Iterate through all of the contract types
            foreach (Type t in _contractAsm.GetExportedTypes())
            {
                //Check to see if the type is a contract
                if (typeof(IContract).IsAssignableFrom(t))
                {
                    bool activatable = false;
                    //Check to see if type is an activatable contract
                    foreach (object obj in t.GetCustomAttributes(false))
                    {
                        if (obj.GetType().Equals(typeof(System.AddIn.Pipeline.AddInContractAttribute)))
                        {
                            activatable = true;
                            break;
                        }
                    }
                    PipelineHints.PipelineSegment customSettings = PipelineHints.PipelineSegment.None;
                    if (t.GetCustomAttributes(typeof(PipelineHints.CustomPipelineAttribute), false).Length > 0)
                    {
                        customSettings = ((PipelineHints.CustomPipelineAttribute)t.GetCustomAttributes(typeof(PipelineHints.CustomPipelineAttribute), false)[0]).Segment;
                    }
                    //Build host, add-in views, and shared views
                    if ((customSettings & PipelineHints.PipelineSegment.AddInView) != PipelineHints.PipelineSegment.AddInView)
                    {
                        BuildView(t, _aib, SegmentType.AIB, activatable);
                    }
                    if ((customSettings & PipelineHints.PipelineSegment.HostView) != PipelineHints.PipelineSegment.HostView)
                    {
                        BuildView(t, _hav, SegmentType.HAV, activatable);
                    }
                    if ((customSettings & PipelineHints.PipelineSegment.Views) != PipelineHints.PipelineSegment.Views)
                    {
                        BuildView(t, _view, SegmentType.VIEW, activatable);
                    }
                    //Build add-in side adapters
                    if ((customSettings & PipelineHints.PipelineSegment.AddInSideAdapter) != PipelineHints.PipelineSegment.AddInSideAdapter)
                    {
                        BuildViewToContractAdapter(t, _asa, SegmentType.ASA, activatable);
                        BuildContractToViewAdapter(t, _asa, SegmentType.ASA, false);
                    }
                    BuildStaticAdapters(t, _asa, SegmentType.ASA);
                    //Build host side adapters
                    if ((customSettings & PipelineHints.PipelineSegment.HostSideAdapter) != PipelineHints.PipelineSegment.HostSideAdapter)
                    {
                        BuildViewToContractAdapter(t, _hsa, SegmentType.HSA, false);
                        BuildContractToViewAdapter(t, _hsa, SegmentType.HSA, activatable);
                    }
                    BuildStaticAdapters(t, _hsa, SegmentType.HSA);
                }
                else if (t.IsEnum)
                {
                    //If type is an enum build adapters and view for that
                    BuildEnumView(t, _aib, SegmentType.AIB);
                    BuildEnumView(t, _hav, SegmentType.HAV);
                    BuildEnumView(t, _view, SegmentType.VIEW);
                    BuildStaticAdapters(t, _hsa, SegmentType.HSA);
                    BuildStaticAdapters(t, _asa, SegmentType.ASA);
                }
            }
            return components;
        }

        private bool ShouldShareViews()
        {
            return _contractAsm.GetCustomAttributes(typeof(PipelineHints.ShareViews), false).Length > 0;
        }


        internal void BuildUpCastableTypeHierarchy()
        {
            foreach (Type t in _contractAsm.GetExportedTypes())
            {
                if (typeof(IContract).IsAssignableFrom(t))
                {
                    Type baseContract = GetBaseContract(t);
                    if (baseContract != null && IsUpCastable(baseContract))
                    {
                        List<Type> siblings;
                        if (_typeHierarchy.TryGetValue(baseContract, out siblings))
                        {
                            siblings.Add(t);
                        }
                        else
                        {
                            siblings = new List<Type>();
                            siblings.Add(t);
                            _typeHierarchy.Add(baseContract, siblings);
                        }
                    }
                }
            }
        }

        internal bool IsUpCastable(Type t)
        {
            return t.GetCustomAttributes(typeof(PipelineHints.AllowViewUpCasting), false).Length > 0;
        }

        /// <summary>
        /// Build the view for an enum. This essentially creates a mirror image of the enum and it's values using the proper names. 
        /// </summary>
        /// <param name="contractType">Input enum type</param>
        /// <param name="component">The pipeline component this source should be part of</param>
        /// <param name="viewType">Either AIB or HAV</param>
        internal void BuildEnumView(Type contractType, PipelineSegmentSource component, SegmentType viewType)
        {
            CodeCompileUnit ccu = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(_symbols.GetNameSpace(viewType,contractType));
            CodeTypeDeclaration type = new CodeTypeDeclaration(_symbols.GetNameFromType(contractType,viewType,SegmentDirection.None,false));
            type.Attributes = MemberAttributes.Public;
            type.IsEnum = true;
            foreach (FieldInfo fi in contractType.GetFields())
            {
                if (!fi.Name.Equals("value__"))
                {
                    CodeMemberField field = new CodeMemberField(fi.FieldType, fi.Name);
                    field.InitExpression = new CodePrimitiveExpression(fi.GetRawConstantValue());
                    type.Members.Add(field);
                }
            }
            if (contractType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration("System.Flags"));
            }
            codeNamespace.Types.Add(type);
            ccu.Namespaces.Add(codeNamespace);
            component.Files.Add(new SourceFile(type.Name, ccu));
        }

        internal static bool IsNativeHandle(Type t)
        {
            return (t.Equals(typeof(System.AddIn.Contract.INativeHandleContract)));
        }

        internal static CodeTypeReference GetNativeHandleViewType(SegmentDirection direction)
        {
            return new CodeTypeReference(typeof(System.Windows.FrameworkElement));
        }

        /// <summary>
        /// Build the view types for a standard contract
        /// </summary>
        /// <param name="contractType">Type of input contract</param>
        /// <param name="component">Pipeline component the source should be added to</param>
        /// <param name="componentType">Pipeline view type (hav or AIB or generic) </param>
        /// <param name="activatable">Is this type an activatable contract</param>
        internal void BuildView(Type contractType, PipelineSegmentSource component, SegmentType componentType,bool activatable)
        {
            if (IsEvent(contractType))
            {
                //Contract type is an event contract and does not have a corresponding view
                return;
            }
            String typeName = _symbols.GetNameFromType(contractType, componentType,SegmentDirection.None,false);
            CodeCompileUnit ccu = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(_symbols.GetNameSpace(componentType,contractType));
            CodeTypeDeclaration type = new CodeTypeDeclaration(typeName);
            type.TypeAttributes = TypeAttributes.Abstract | TypeAttributes.Public;
            object[] typeComments = contractType.GetCustomAttributes(typeof(PipelineHints.CommentAttribute), false);
            foreach (PipelineHints.CommentAttribute comment in typeComments)
            {
                type.Comments.Add(new CodeCommentStatement(comment.Comment));
            }
            if (IsViewInterface(contractType))
            {
                type.TypeAttributes |= TypeAttributes.Interface;
            }

            //This will consult the type hierarchy we built earlier, currently we only support one base type or 1 implemented interface
            Type baseType = GetBaseContract(contractType);
            if (baseType != null)
            {
                CodeTypeReference baseRef = new CodeTypeReference(_symbols.GetNameFromType(baseType, componentType, SegmentDirection.None, contractType));
                type.BaseTypes.Add(baseRef);
            }
            if (IsEventArgs(contractType))
            {
                //Contract type is an event args type and needs it as a base
                PipelineHints.EventArgsAttribute argsType = GetEventArgs(contractType);
                if (argsType.Cancelable)
                {
                    type.BaseTypes.Add(typeof(System.ComponentModel.CancelEventArgs));
                }
                else
                {
                    type.BaseTypes.Add(typeof(EventArgs));
                }
            }
            //Only the add-in base and shared views need an attribute, the HAV doesn't need one. 
            if (activatable && (componentType == SegmentType.AIB || componentType == SegmentType.VIEW))
            {
                CodeAttributeDeclaration marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.AddIn.Pipeline.AddInBaseAttribute)));
                type.CustomAttributes.Add(marker);
            }
            Dictionary<String, CodeMemberProperty> props = new Dictionary<string, CodeMemberProperty>();
            foreach (MethodInfo mi in GetMethodsFromContract(contractType,false))
            {
                //We only need to build the event once, so we decided to do it on event add.
                //We do not do error checking to match up adds and removes.
                if (IsEventAdd(mi))
                {

                    CodeTypeReference eventType = GetEventViewType(componentType, mi,false);
                    CodeMemberEvent abstractEvent = new CodeMemberEvent();
                    PipelineHints.EventAddAttribute attr = GetEventAdd(mi);
                    //TODO: remove this line. Abstract events are not supported by codedom since VB can't handle them.
                    abstractEvent.Attributes = MemberAttributes.Abstract;
                    abstractEvent.Name = attr.Name;
                    abstractEvent.Type = eventType;
                    type.Members.Add(abstractEvent);
                    //We only look for comments on the event add method. Any comments on the event remove method will be ignored
                    object[] eventComments = mi.GetCustomAttributes(typeof(PipelineHints.CommentAttribute), false);
                    foreach (PipelineHints.CommentAttribute comment in eventComments)
                    {
                        abstractEvent.Comments.Add(new CodeCommentStatement(comment.Comment));
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
                    prop = GetProperyDecl(contractType,type,mi,props,componentType, direction, prefix);                   
                    switch (GetPropertyAttribute(mi).Type)
                    {
                        case PropertyType.set:
                            prop.HasSet = true;
                            break;
                        case PropertyType.get:
                            prop.HasGet = true;
                            break;
                    }
                    object[] propComments = mi.GetCustomAttributes(typeof(PipelineHints.CommentAttribute), false);
                    foreach (PipelineHints.CommentAttribute comment in propComments)
                    {
                        prop.Comments.Add(new CodeCommentStatement(comment.Comment));
                    }
                    continue;
                   
                    
                }
                CodeMemberMethod method = new CodeMemberMethod();
                method.Attributes = MemberAttributes.Abstract | MemberAttributes.Public;
                method.Name = mi.Name;
                //Setup the return type for this member in the view
                method.ReturnType = GetMethodReturnTypeForView(componentType, mi);
                //For each parameter in the method in the contract, add the right one to the view
                AddParametersToViewMethod(componentType, mi, method);
                object[] methodComments = mi.GetCustomAttributes(typeof(PipelineHints.CommentAttribute), false);
                foreach (PipelineHints.CommentAttribute comment in methodComments)
                {
                    method.Comments.Add(new CodeCommentStatement(comment.Comment));
                }
                type.Members.Add(method);
            }
            codeNamespace.Types.Add(type);
            ccu.Namespaces.Add(codeNamespace);
            component.Files.Add(new SourceFile(typeName, ccu));
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
                //If the type doesn't need adapting just place it in the method, else we check to see why it needs adapting (is it a "known" type) and
                //add a paramter of the appropriate type. 
                if (!TypeNeedsAdapting(pi.ParameterType))
                {
                    CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                    method.Parameters.Add(cp);
                }
                else
                {
                    if (IsIListContract(pi.ParameterType))
                    {
                        CodeTypeReference paramType = GetIListContractTypeRef(componentType, SegmentDirection.None,  pi.ParameterType,mi.DeclaringType);
                        method.Parameters.Add(new CodeParameterDeclarationExpression(paramType, pi.Name));
                    }
                    else if (IsNativeHandle(pi.ParameterType))
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(GetNativeHandleViewType(SegmentDirection.ContractToView), pi.Name));
                    }
                    else
                    {
                        CodeTypeReference paramType =
                                new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.None, mi.DeclaringType));
                        CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(paramType, pi.Name);
                        method.Parameters.Add(cp);
                    }

                }
            }
        }

        private CodeTypeReference GetMethodReturnTypeForView(SegmentType componentType, MethodInfo mi)
        {
            //If the return value does not need adapting set the return value as the actual type. 
            //If it needs adapting but is not an IlistContract set the return value as the view type for the specified return value, 
            //otherwise set the return type as IList<TView> for the IListContract<TContract>
            if (!TypeNeedsAdapting(mi.ReturnType))
            {
                return new CodeTypeReference(mi.ReturnType);
            }
            else
            {
                if (IsIListContract(mi.ReturnType))
                {
                    SegmentDirection direction = SegmentDirection.None;
                    Type contractGenericType = mi.ReturnType;
                    CodeTypeReference returnType = GetIListContractTypeRef(componentType, direction,  contractGenericType,mi.DeclaringType);
                    return returnType;
                }
                else if (IsNativeHandle(mi.ReturnType))
                {
                    return GetNativeHandleViewType(SegmentDirection.ViewToContract);
                }
                else
                {
                    return new CodeTypeReference(_symbols.GetNameFromType(mi.ReturnType, componentType, SegmentDirection.None, mi.DeclaringType));
                }


            }
        }


        /// <summary>
        /// Build the CodeTypeReference for an Event on the view
        /// </summary>
        /// <param name="componentType">Which type of view: addin, host, shared?</param>
        /// <param name="mi">MethodInfo from the original contract</param>
        /// <param name="fullyQualified">Should we use a namespace prefix for the view type</param>
        /// <returns></returns>
        private CodeTypeReference GetEventViewType(SegmentType componentType, MethodInfo mi,bool fullyQualified)
        {
            CodeTypeReference viewArgsType = GetEventArgsType(componentType, mi, fullyQualified);
            CodeTypeReference eventType = new CodeTypeReference(typeof(EventHandler<>));
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
            return !((contractType.GetCustomAttributes(typeof(PipelineHints.AbstractBaseClass), false).Length > 0) ||
                      IsEventArgs(contractType) ||
                      contractType.IsEnum);
        }




        //A type has been specified as an event args type iff it has the event args attribute applied to its contract
        private static bool IsEventArgs(Type contractType)
        {
            return contractType.GetCustomAttributes(typeof(PipelineHints.EventArgsAttribute), false).Length > 0;
        }

        private static PipelineHints.EventArgsAttribute GetEventArgs(Type contractType)
        {
            try
            {
                return (PipelineHints.EventArgsAttribute)contractType.GetCustomAttributes(typeof(PipelineHints.EventArgsAttribute), false)[0];
            }
            catch (IndexOutOfRangeException)
            {
                throw new InvalidOperationException("Tried to get the event args attribute from a type that is not marked with that attribute: " + contractType.FullName);
            }
        }

        private CodeTypeReference GetIListContractTypeRef(SegmentType componentType, SegmentDirection direction,  Type contractGenericType,Type referenceType)
        {
            try
            {
                Type genericParameter = contractGenericType.GetGenericArguments()[0];
                CodeTypeReference returnType = new CodeTypeReference(typeof(IList<>));
                returnType.TypeArguments.Add(new CodeTypeReference(_symbols.GetNameFromType(genericParameter, componentType, direction, referenceType)));
                return returnType;
            }
            catch (IndexOutOfRangeException)
            {
                throw new InvalidOperationException("Tried to get the generic arguments for a type that does not have them: " + contractGenericType.FullName);
            }
        }

        //Since multiple methods on the contract correspond to one property type on the view (in the case of a property with a getter and a setter we need to keep
        //track of which properties have been created before to decide whether we need to create a new one or add either a gettter or setter to an existing one. 
        //This method looks in a global hashtable for the property it needs and if it doesn't find one it creates one and inserts it into the table. 
        internal CodeMemberProperty GetProperyDecl(Type contractType, CodeTypeDeclaration type, MethodInfo mi, Dictionary<String, CodeMemberProperty> props, SegmentType componentType, SegmentDirection direction, bool prefix)
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
                    prop.Type = new CodeTypeReference(mi.ReturnType);
                }
                else
                {
                    if (IsIListContract(propertyType))
                    {
                        prop.Type = GetIListContractTypeRef(componentType, direction,  propertyType,mi.DeclaringType);
                    }
                    else if (IsNativeHandle(propertyType))
                    {
                        prop.Type = GetNativeHandleViewType(SegmentDirection.ViewToContract);
                    }
                    else
                    {
                        prop.Type = new CodeTypeReference(_symbols.GetNameFromType(propertyType, componentType, direction, contractType));
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
                    {
                        throw new InvalidOperationException("Property setter indicated on a method that has more than one input parameter: " + mi.Name + " on " + mi.ReflectedType.Name);
                    }else
                    {
                        return mi.GetParameters()[0].ParameterType;
                    }
                  }
                default:
                    return null;
            }
           
        }

        private static bool IsIListContract(Type t)
        {
            if (t.GetGenericArguments().Length > 0)
            {
                return t.GetGenericTypeDefinition().Equals(typeof(System.AddIn.Contract.IListContract<>));
            }
            else
            {
                return false;
            }
        }

        private static Type GetListGenericParamterType(Type t)
        {
            if (t.GetGenericArguments().Length == 1)
            {
                return t.GetGenericArguments()[0];
            }
            else
            {
                return null;
            }
        }

        private static bool IsEventAdd(MethodInfo mi)
        {
            return mi.GetCustomAttributes(typeof(PipelineHints.EventAddAttribute), false).Length > 0;
        }

        private static bool IsEventRemove(MethodInfo mi)
        {
            return mi.GetCustomAttributes(typeof(PipelineHints.EventRemoveAttribute), false).Length > 0;
        }


        private static PipelineHints.EventAddAttribute GetEventAdd(MethodInfo mi)
        {
            if (IsEventAdd(mi))
            {
                return (PipelineHints.EventAddAttribute)mi.GetCustomAttributes(typeof(PipelineHints.EventAddAttribute),false)[0];
            }
            else
            {
                return null;
            }
        }

        private static PipelineHints.EventRemoveAttribute GetEventRemove(MethodInfo mi)
        {
            if (IsEventRemove(mi))
            {
                return (PipelineHints.EventRemoveAttribute)mi.GetCustomAttributes(typeof(PipelineHints.EventRemoveAttribute), false)[0];
            }
            else
            {
                return null;
            }
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
                mi.ReturnType.Equals(typeof(void)) &&
                mi.GetParameters().Length == 1)
            {
                return true;
            }
            //getter
            if (mi.Name.StartsWith("get_") &&
                !mi.ReturnType.Equals(typeof(void)) &&
                mi.GetParameters().Length == 0)
            {
                return true;
            }
            return false;
        }

        internal static string GetViewNameFromMethod(MethodInfo mi)
        {
            if (IsProperty(mi))
            {
                PropertyMethodInfo prop = GetPropertyAttribute(mi);
                return prop.Name;
            }
            else
            {
                return mi.Name;
            }
        }


        //For a given method, marked as an event, get a type reference for the event args type for the view.
        private CodeTypeReference GetEventArgsType(SegmentType componentType, MethodInfo mi, bool prefix)
        {
            //The fist and only parameter should be for contract representing the delegate type
            if (mi.GetParameters().Length != 1)
            {
                throw new InvalidOperationException("A method specified as an event does has the wrong number of parameters (not 1): " + mi.Name + " on " + mi.ReflectedType.Name);
            }
            ParameterInfo pi = mi.GetParameters()[0];
            //The contract representing the delegate type should have exactly one method
            if (pi.ParameterType.GetMethods().Length != 1)
            {
                throw new InvalidOperationException("A type specified as an event delegate does has the wrong number of methods (not 1): " + pi.Name);
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
                    throw new InvalidOperationException("More than one parameters have been specified for an event delegate: " + eventMethod.Name + " on " + eventMethod.ReflectedType.Name);
                }
                pi = eventMethod.GetParameters()[0];
                String typeName = _symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.None, prefix);
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
            if (contractType.GetCustomAttributes(typeof(PipelineHints.EventHandlerAttribute), false).Length > 0)
            {
                return;
            }
            String typeName = _symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, false);
            CodeCompileUnit ccu = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(_symbols.GetNameSpace(componentType,contractType));
            CodeTypeDeclaration type = new CodeTypeDeclaration(typeName);
            type.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
            SegmentType viewComponentType;
            if (componentType == SegmentType.ASA)
            {
                viewComponentType = SegmentType.AIB;
            }
            else if (componentType == SegmentType.HSA)
            {
                viewComponentType = SegmentType.HAV;
            }
            else
            {
                throw new InvalidOperationException("Wrong component type");
            }
            CodeTypeReference viewType = new CodeTypeReference(_symbols.GetNameFromType(contractType,viewComponentType,SegmentDirection.None,true));
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

        private CodeMemberMethod CreateContractToViewStaticAdapter(Type contractType, SegmentType componentType, CodeTypeReference viewType)
        {
            CodeMemberMethod cva = new CodeMemberMethod();
            cva.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
            CodeTypeReference adapterType = new CodeTypeReference(_symbols.GetNameFromType(contractType, componentType, SegmentDirection.ContractToView,false));
            cva.Name = _symbols.GetStaticAdapterMethodNameName(contractType, componentType, SegmentDirection.ContractToView);
            cva.ReturnType = viewType;
            CodeParameterDeclarationExpression contractParam = new CodeParameterDeclarationExpression(contractType, "contract");
            cva.Parameters.Add(contractParam);
            if (!contractType.IsEnum)
            {
                #region TryUpCast
                List<Type> subTypes;
                if (_typeHierarchy.TryGetValue(contractType, out subTypes))
                {
                    cva.Statements.Add(new CodeVariableDeclarationStatement(typeof(IContract), "subContract"));
                    CodeVariableReferenceExpression subContractRef = new CodeVariableReferenceExpression("subContract");
                    foreach (Type t in subTypes)
                    {
                        CodeAssignStatement assign = new CodeAssignStatement();
                        CodeMethodInvokeExpression queryContract = 
                            new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("contract"), 
                                                           "QueryContract", 
                                                           new CodePrimitiveExpression(t.FullName));
                        assign.Left = subContractRef;
                        assign.Right = queryContract;
                        CodeConditionStatement ifContractFound = new CodeConditionStatement();
                        CodeBinaryOperatorExpression nullCheck = 
                            new CodeBinaryOperatorExpression (subContractRef, 
                                                             CodeBinaryOperatorType.IdentityInequality, 
                                                             new CodePrimitiveExpression(null));
                        ifContractFound.Condition = nullCheck;
                        CodeCastExpression castContract = new CodeCastExpression(t, subContractRef);
                        CodeMethodInvokeExpression subTypeAdapter = CallStaticAdapter(componentType, t, castContract, SegmentDirection.ContractToView);
                        ifContractFound.TrueStatements.Add(new CodeMethodReturnStatement(subTypeAdapter));
                        cva.Statements.Add(assign);
                        cva.Statements.Add(ifContractFound);
                    }
                    
                }
                #endregion


                String outgoingAdapterName = _symbols.GetNameFromType(contractType, componentType, SegmentDirection.ViewToContract, false);
                CodeConditionStatement tryCast = new CodeConditionStatement();
                CodeVariableReferenceExpression contract = new CodeVariableReferenceExpression("contract");
                //Create a new ViewToContractAdapter and pass out
                CodeObjectCreateExpression adapterNew = new CodeObjectCreateExpression(adapterType, contract);
                tryCast.FalseStatements.Add(new CodeMethodReturnStatement(adapterNew));
                //Cast to our ViewToContractAdapter and return original view
                CodeCastExpression cast = new CodeCastExpression(outgoingAdapterName, contract);
                CodeMethodInvokeExpression getContract = new CodeMethodInvokeExpression(cast, "GetSourceView");
                tryCast.TrueStatements.Add(new CodeMethodReturnStatement(getContract));
                //Create the type check
                CodeMethodInvokeExpression getType = new CodeMethodInvokeExpression(contract, "GetType");
                CodeMethodInvokeExpression equals = new CodeMethodInvokeExpression(getType, "Equals");
                CodeTypeOfExpression typeofExpr = new CodeTypeOfExpression(outgoingAdapterName);
                equals.Parameters.Add(typeofExpr);
                //Check to see if it's a local object
                CodeMethodInvokeExpression isRemote =
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(System.Runtime.Remoting.RemotingServices)), "IsObjectOutOfAppDomain", contract);
                CodeBinaryOperatorExpression canUnwrap = new CodeBinaryOperatorExpression();
                canUnwrap.Operator = CodeBinaryOperatorType.BooleanAnd;
                canUnwrap.Right = equals;
                canUnwrap.Left = new CodeBinaryOperatorExpression(isRemote, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(true));
                
                tryCast.Condition = canUnwrap;

                cva.Statements.Add(tryCast);
            }
            else
            {
                CodeCastExpression cce = new CodeCastExpression(viewType, new CodeVariableReferenceExpression("contract"));
                cva.Statements.Add(new CodeMethodReturnStatement(cce));
            }
            return cva;
        }

        private CodeMemberMethod CreateViewToContractStaticAdapter(Type contractType, SegmentType componentType, CodeTypeReference viewType)
        {
            CodeMemberMethod vca = new CodeMemberMethod();
            vca.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
            CodeTypeReference adapterType = new CodeTypeReference(_symbols.GetNameFromType(contractType, componentType, SegmentDirection.ViewToContract, false));
            vca.Name = _symbols.GetStaticAdapterMethodNameName(contractType, componentType, SegmentDirection.ViewToContract);
            vca.ReturnType = new CodeTypeReference(contractType);
            CodeParameterDeclarationExpression viewParam = new CodeParameterDeclarationExpression(viewType, "view");
            vca.Parameters.Add(viewParam);
            if (contractType.IsEnum)
            {
                CodeCastExpression cce = new CodeCastExpression(contractType, new CodeVariableReferenceExpression("view"));
                vca.Statements.Add(new CodeMethodReturnStatement(cce));
            }
            
            else
            {
                String incomingAdapterName = _symbols.GetNameFromType(contractType, componentType, SegmentDirection.ContractToView, false);
                CodeConditionStatement tryCast = new CodeConditionStatement();
                CodeVariableReferenceExpression view = new CodeVariableReferenceExpression("view");
                //Create a new ViewToContractAdapter and pass out
                CodeObjectCreateExpression adapterNew = new CodeObjectCreateExpression(adapterType, view);
                tryCast.FalseStatements.Add(new CodeMethodReturnStatement(adapterNew));
                //Cast to our ContractToViewAdapter and return original contract
                CodeCastExpression cast = new CodeCastExpression(incomingAdapterName, view);
                CodeMethodInvokeExpression getContract = new CodeMethodInvokeExpression(cast, "GetSourceContract");
                tryCast.TrueStatements.Add(new CodeMethodReturnStatement(getContract));
                //Create the type check
                CodeMethodInvokeExpression getType = new CodeMethodInvokeExpression(view, "GetType");
                CodeMethodInvokeExpression equals = new CodeMethodInvokeExpression(getType, "Equals");
                CodeTypeOfExpression typeofExpr = new CodeTypeOfExpression(incomingAdapterName);
                equals.Parameters.Add(typeofExpr);
                tryCast.Condition = equals;
                vca.Statements.Add(tryCast);
            }
            return vca;
        }

        internal void BuildViewToContractAdapter(Type contractType, PipelineSegmentSource component,SegmentType componentType,bool activatable)
        {
            //Set up type
            String typeName = _symbols.GetNameFromType(contractType, componentType,SegmentDirection.ViewToContract,false);
            Dictionary<String, CodeMemberProperty> props = new Dictionary<string, CodeMemberProperty>();
            SegmentType viewType;
            if (componentType == SegmentType.ASA)
            {
                viewType = SegmentType.AIB;
            }
            else
            {
                viewType = SegmentType.HAV;
            }
            String viewName =  _symbols.GetNameFromType(contractType,viewType);
            //If this is an event type determine which real type this is an event on
            if (contractType.GetCustomAttributes(typeof(PipelineHints.EventHandlerAttribute), false).Length > 0)
            {
                viewName = "System.Object";
            }
            //Set up the namespace and the type declaration
            //Derive from contractbase and the specific contract
            CodeCompileUnit ccu = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(_symbols.GetNameSpace(componentType,contractType));
            CodeTypeDeclaration type = new CodeTypeDeclaration(typeName);
            type.TypeAttributes = TypeAttributes.Public;
            type.BaseTypes.Add(new CodeTypeReference(typeof(ContractBase)));
            type.BaseTypes.Add(new CodeTypeReference(contractType));
            //If this is activatable mark it with the addinadapterattribute
            //The viewtocontract adapter is only ever activatable in the add-in side adapter so no need to check which adapter we're in
            if (activatable)
            {
                CodeAttributeDeclaration marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.AddIn.Pipeline.AddInAdapterAttribute)));
                type.CustomAttributes.Add(marker);
            }
            CodeMemberField aib = new CodeMemberField(viewName, "_view");
            type.Members.Add(aib);
            //Build constructor
            //Add parameter for view type and assign it to member field _view
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression(viewName, "view");
            constructor.Parameters.Add(parameter);
            CodeAssignStatement assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_view"), new CodeVariableReferenceExpression("view"));
            constructor.Statements.Add(assign);
            type.Members.Add(constructor);
            if (IsEvent(contractType))
            {
                //If this is an event type we have an additional constructor paramter that is the fieldinfo object for the eventhandler we need to invoke. 
                //Add this parameter to the constructor and then store it in a member variable.
                CodeMemberField eventMember = new CodeMemberField(typeof(System.Reflection.MethodInfo), "_event");
                eventMember.Attributes |= MemberAttributes.Private;
                type.Members.Add(eventMember);
                constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(System.Reflection.MethodInfo), "eventProp"));
                constructor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("_event"), new CodeVariableReferenceExpression("eventProp")));
                //We have already validated that this is an event so the attribute exists
                //We have also validated, while creating the views, that this contract has one method and that one method takes in one parameter
                //The one method on this type acts as the delegate with its parameter representing the event args.
                MethodInfo mi = contractType.GetMethods()[0];
                CodeExpression args = null;
                CodeTypeReference parameterType;
                ParameterInfo pi = mi.GetParameters()[0];
                parameterType = new CodeTypeReference(pi.ParameterType);

                CodeMemberMethod method = new CodeMemberMethod();
                method.Name = mi.Name;
                method.Parameters.Add(new CodeParameterDeclarationExpression(parameterType, "args"));
                method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                CodeStatementCollection adaptArgs = new CodeStatementCollection();
                //If the parameter type needs to be adapted to pass it to the contract then new up an adapter and pass that along as a parameter
                //Else simply pass the args in directly as the parameter. 
                CodeTypeReference eventArgsViewType;
                if (TypeNeedsAdapting(pi.ParameterType))
                {
                    CodeObjectCreateExpression adaptedArgs = new CodeObjectCreateExpression();
                    adaptedArgs.Parameters.Add(new CodeVariableReferenceExpression("args"));
                    adaptedArgs.CreateType = new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ContractToView, false));
                    CodeVariableDeclarationStatement adapterArgsDeclare = new CodeVariableDeclarationStatement(adaptedArgs.CreateType, "adaptedArgs");
                    CodeAssignStatement assignArgs = new CodeAssignStatement(new CodeVariableReferenceExpression("adaptedArgs"), adaptedArgs);
                    adaptArgs.Add(adapterArgsDeclare);
                    adaptArgs.Add(assignArgs);
                    args = new CodeVariableReferenceExpression("adaptedArgs");
                    eventArgsViewType = new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, viewType));
                }
                else
                {
                    args = new CodeVariableReferenceExpression("args");
                    eventArgsViewType = new CodeTypeReference(pi.ParameterType);
                }


                method.Statements.AddRange(adaptArgs);
                CodeVariableDeclarationStatement argsArray = 
                    new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(object[])), "argsArray");
                argsArray.InitExpression = 
                    new CodeArrayCreateExpression(new CodeTypeReference(typeof(object)), new CodePrimitiveExpression(1));
                CodeAssignStatement addToArgsArray = new CodeAssignStatement();
                addToArgsArray.Left = 
                    new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("argsArray"),new CodePrimitiveExpression(0));
                addToArgsArray.Right = args;
                CodeMethodInvokeExpression eventInvoke = new CodeMethodInvokeExpression();
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
                if (mi.ReturnType.Equals(typeof(bool)))
                {
                    method.ReturnType = new CodeTypeReference(typeof(bool));
                    CodeFieldReferenceExpression cancel = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("adaptedArgs"), "Cancel");
                    CodeMethodReturnStatement retCancelCheck = new CodeMethodReturnStatement(cancel);
                    method.Statements.Add(retCancelCheck);
                  
                }

                type.Members.Add(method);
            }
            else
            {
                //For standard contract types we simply iterate through each method and build an adapter one by one
                foreach (MethodInfo mi in GetMethodsFromContract(contractType, true))
                {

                    CodeMemberMethod method = new CodeMemberMethod();
                    CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
                    CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
                    cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_view"), mi.Name);
                    method.Attributes = MemberAttributes.Public;
                    method.Name = mi.Name;
                    //Set the methods return type appropriately
                    if (mi.ReturnType.Equals(typeof(void)))
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
                        if (IsIListContract(mi.ReturnType))
                        {
                            //If the return type is an IListContract<T> call the method and pass its result to the adapters for IListContract<T>
                            Type genericParam = GetListGenericParamterType(mi.ReturnType);
                            ret.Expression = CallListAdapter(SegmentDirection.ViewToContract, componentType, viewType, cmi, genericParam);
                        }
                        else
                        {
                            //If the return type needs adapting call method, pass its return value to the static adapter, and then return that
                            CodeMethodInvokeExpression adaptExpr = CallStaticAdapter(componentType, mi.ReturnType, cmi, SegmentDirection.ViewToContract);
                            ret.Expression = adaptExpr;
                        }

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
                        PipelineHints.EventAddAttribute attr = GetEventAdd(mi);
                        CodeTypeReference eventArgsType = GetEventArgsType(viewType, mi, true);
                        ParameterInfo pi = mi.GetParameters()[0];
                        //Build a reference to the event handler on the view object
                        CodeEventReferenceExpression eventHandler =
                            new CodeEventReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
                        //Get the adapter for the event args type
                        CodeTypeReference adapterType =
                            new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ContractToView, false));
                        CodeObjectCreateExpression adapterConstruct = new CodeObjectCreateExpression(adapterType, new CodeVariableReferenceExpression(pi.Name));
                        CodeDelegateCreateExpression handler = new CodeDelegateCreateExpression();
                        //Build the event handler
                        handler.DelegateType = new CodeTypeReference(typeof(EventHandler<>));
                        handler.DelegateType.TypeArguments.Add(eventArgsType);
                        handler.TargetObject = adapterConstruct;
                        handler.MethodName = "Handler";
                        CodeVariableDeclarationStatement handlerVar = new CodeVariableDeclarationStatement();
                        handlerVar.Type = handler.DelegateType;
                        handlerVar.Name = "adaptedHandler";
                        handlerVar.InitExpression = handler;
                        method.Statements.Add(handlerVar);
                        //Add attach the handler to the eventhandler type on the view and finish building the method
                        CodeAttachEventStatement attach = new CodeAttachEventStatement(eventHandler, new CodeVariableReferenceExpression(handlerVar.Name));
                        CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                        method.Parameters.Add(cp);
                        method.Statements.Add(attach);

                        //Add Dictionary of adapters
                        CodeTypeReference dictionaryType = new CodeTypeReference("System.Collections.Generic.Dictionary");
                        dictionaryType.TypeArguments.Add(new CodeTypeReference(pi.ParameterType));
                        dictionaryType.TypeArguments.Add(handler.DelegateType);
                        CodeMemberField handlers = new CodeMemberField();
                        handlers.Name = attr.Name + "_handlers";
                        handlers.Type = dictionaryType;
                        type.Members.Add(handlers);
                        //Initialize Dictionary of Adapters;
                        CodeObjectCreateExpression createDictionary = new CodeObjectCreateExpression();
                        createDictionary.CreateType = dictionaryType;
                        CodeAssignStatement initDictionary = new CodeAssignStatement();
                        initDictionary.Left = new CodeVariableReferenceExpression(handlers.Name);
                        initDictionary.Right = createDictionary;
                        constructor.Statements.Add(initDictionary);
                        //Add current handler to the dictionary
                        CodeAssignStatement storeHandler = new CodeAssignStatement();
                        CodeArrayIndexerExpression dictLocation = new CodeArrayIndexerExpression();
                        dictLocation.TargetObject = new CodeVariableReferenceExpression(handlers.Name);
                        dictLocation.Indices.Add(new CodeVariableReferenceExpression(pi.Name));
                        storeHandler.Left = dictLocation;
                        storeHandler.Right = new CodeVariableReferenceExpression(handlerVar.Name);
                        method.Statements.Add(storeHandler);

                    }
                    else if (IsEventRemove(mi))
                    {
                        PipelineHints.EventRemoveAttribute attr = GetEventRemove(mi);
                        ParameterInfo pi = mi.GetParameters()[0];
                        //Declare the handler 

                        CodeTypeReference eventArgsType = GetEventArgsType(viewType, mi, true);
                        CodeVariableDeclarationStatement handlerVar = new CodeVariableDeclarationStatement();
                        handlerVar.Name = "adaptedHandler";
                        handlerVar.Type = new CodeTypeReference(typeof(EventHandler<>));
                        handlerVar.Type.TypeArguments.Add(eventArgsType);
                        method.Statements.Add(handlerVar);
                        //TryGet handler from handlers
                        CodeMethodInvokeExpression tryGet = new CodeMethodInvokeExpression();
                        tryGet.Method.TargetObject = new CodeVariableReferenceExpression(attr.Name + "_handlers");
                        tryGet.Method.MethodName = "TryGetValue";
                        tryGet.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
                        tryGet.Parameters.Add(new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(handlerVar.Name)));

                        CodeConditionStatement ifGotValue = new CodeConditionStatement();
                        ifGotValue.Condition = tryGet;
                        //Remove handler
                        CodeMethodInvokeExpression removeHandler = new CodeMethodInvokeExpression();
                        removeHandler.Method.MethodName = "Remove";
                        removeHandler.Method.TargetObject = new CodeVariableReferenceExpression(attr.Name + "_handlers");
                        removeHandler.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
                        ifGotValue.TrueStatements.Add(removeHandler);
                        CodeRemoveEventStatement detach = new CodeRemoveEventStatement();
                        detach.Event = new CodeEventReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
                        detach.Listener = new CodeVariableReferenceExpression(handlerVar.Name);
                        ifGotValue.TrueStatements.Add(detach);
                        method.Statements.Add(ifGotValue);
                        //Add parameters to method decl
                        CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                        method.Parameters.Add(cp);
                    }
                    else
                    {
                        //This is a standard method to adapt, go through each parameter, check to see if it needs adapting. 
                        //If no adapting is needed just pass on through, else find the right adapter and use it
                        foreach (ParameterInfo pi in mi.GetParameters())
                        {
                            if (!TypeNeedsAdapting(pi.ParameterType))
                            {
                                CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                                method.Parameters.Add(cp);
                                cmi.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
                            }
                            else
                            {

                                CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                                method.Parameters.Add(cp);
                                CodeMethodInvokeExpression adapterExpr = null;
                                if (!IsIListContract(pi.ParameterType))
                                {
                                    adapterExpr =
                                        CallStaticAdapter(componentType, pi.ParameterType, new CodeVariableReferenceExpression(pi.Name), SegmentDirection.ContractToView);

                                }
                                else
                                {
                                    Type genericParamType = GetListGenericParamterType(pi.ParameterType);
                                    adapterExpr = CallListAdapter(SegmentDirection.ContractToView, componentType, viewType, new CodeVariableReferenceExpression(pi.Name), genericParamType);
                                }
                                cmi.Parameters.Add(adapterExpr);
                            }
                        }
                        if (ret != null)
                        {
                            //If the previously computed return statement is not null add it to the method 
                            //It already has the call to cmi's invocation so no need to add cmi again
                            method.Statements.Add(ret);

                        }
                        else
                        {
                            //If the previously computed return statement is null then add cmi directly to the method statements
                            method.Statements.Add(cmi);
                        }
                    }
                    if (method != null)
                    {
                        type.Members.Add(method);
                    }
                }
            }

            //Add the method to unwrap the original view
            CodeMemberMethod unadapt = new CodeMemberMethod();
            unadapt.Name = "GetSourceView";
            unadapt.ReturnType = new CodeTypeReference(viewName);
            unadapt.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
            unadapt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("_view")));
            type.Members.Add(unadapt);


            codeNamespace.Types.Add(type);
            ccu.Namespaces.Add(codeNamespace);
            component.Files.Add(new SourceFile(typeName, ccu));
        }

        private CodeStatement GetPropertyViewToContractAdapterImpl(SegmentType componentType, SegmentType viewType, MethodInfo mi, CodeMemberMethod method)
        {
            PropertyMethodInfo attr = GetPropertyAttribute(mi);
            CodePropertyReferenceExpression prop =
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("_view"), attr.Name);
            if (attr.Type == PropertyType.get)
            {
                CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
                if (!TypeNeedsAdapting(mi.ReturnType))
                {
                    //If the type does not need adapting just pass the return value directly
                    ret.Expression = prop;
                }
                else if (IsIListContract(mi.ReturnType))
                {
                    //If this is an IListContract call the IListContract adapters
                    Type genericParamType = GetListGenericParamterType(mi.ReturnType);
                    ret.Expression = CallListAdapter(SegmentDirection.ViewToContract, componentType, viewType, prop, genericParamType);
                }
                else
                {
                    //If this is a standard custom contract call the appropriate static adapter
                    ret.Expression = CallStaticAdapter(componentType, mi.ReturnType, prop, SegmentDirection.ViewToContract);
                }

               return ret;
            }
            else
            {
                CodeAssignStatement set = new CodeAssignStatement();
                set.Left = prop;
                ParameterInfo pi = mi.GetParameters()[0];
                if (method != null)
                {
                    method.Parameters.Add(new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name));
                }
                if (!TypeNeedsAdapting(pi.ParameterType))
                {
                    //If the type doesn't need adapting just asign it directly
                    set.Right = new CodeVariableReferenceExpression(pi.Name);
                }
                else if (IsIListContract(pi.ParameterType))
                {
                    //If the type is an IListContract<T> then call the IListContract<T> adapters
                    Type genericParamType = GetListGenericParamterType(pi.ParameterType);
                    set.Right = CallListAdapter(SegmentDirection.ContractToView, componentType, viewType, new CodeVariableReferenceExpression(pi.Name), genericParamType);
                }
                else
                {
                    //If this is a standard custom contract call the appropriate static adapter
                    set.Right = CallStaticAdapter(componentType, pi.ParameterType, new CodeVariableReferenceExpression(pi.Name), SegmentDirection.ContractToView);
                }
                return set;
            }
        }

        private CodeStatement GetPropertyContractToViewAdapterImpl(SegmentType componentType, SegmentType viewType, MethodInfo mi, CodeMemberMethod method)
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
                CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
                if (!TypeNeedsAdapting(mi.ReturnType))
                {
                    //If the type does not need adapting just pass the return value directly
                    ret.Expression = prop;
                }
                else if (IsIListContract(mi.ReturnType))
                {
                    //If this is an IListContract call the IListContract adapters
                    Type genericParamType = GetListGenericParamterType(mi.ReturnType);
                    ret.Expression = CallListAdapter(SegmentDirection.ContractToView, componentType, viewType, prop, genericParamType);
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
                CodeVariableReferenceExpression param = new CodeVariableReferenceExpression("value");
                if (method != null)
                {
                    method.Parameters.Add(new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name));
                }
                if (!TypeNeedsAdapting(pi.ParameterType))
                {
                    //If the type doesn't need adapting just asign it directly
                    value = param;
                }
                else if (IsIListContract(pi.ParameterType))
                {
                    //If the type is an IListContract<T> then call the IListContract<T> adapters
                    Type genericParamType = GetListGenericParamterType(pi.ParameterType);
                    value = CallListAdapter(SegmentDirection.ViewToContract, componentType, viewType, param, genericParamType);
                }
                else
                {
                    //If this is a standard custom contract call the appropriate static adapter
                    value = CallStaticAdapter(componentType, pi.ParameterType,param, SegmentDirection.ViewToContract);
                }
                if (IsProperty(mi))
                {
                    CodeAssignStatement set = new CodeAssignStatement();
                    set.Left = prop;
                    set.Right = value;
                    return set;
                }
                else
                {
                    CodeMethodInvokeExpression set = (CodeMethodInvokeExpression) prop;
                    set.Parameters.Add(value);
                    return new CodeExpressionStatement(set);
                }
            }
           
        }

        

        private static bool IsEvent(Type contractType)
        {
            return contractType.GetCustomAttributes(typeof(PipelineHints.EventHandlerAttribute), false).Length > 0;
        }

       

        private CodeMethodInvokeExpression CallListAdapter(SegmentDirection direction, SegmentType componentType, SegmentType viewType, CodeExpression source, Type genericParamType)
        {
            String genericParamViewName = _symbols.GetNameFromType(genericParamType, viewType, SegmentDirection.None, true);
            CodeMethodReferenceExpression ContractToViewAdapter = GetStaticAdapter(componentType, genericParamType, SegmentDirection.ContractToView);
            CodeMethodReferenceExpression ViewToContractAdapter = GetStaticAdapter(componentType, genericParamType, SegmentDirection.ViewToContract);
            CodeMethodInvokeExpression adapterExpr = new CodeMethodInvokeExpression();
            adapterExpr.Method = new CodeMethodReferenceExpression();
            adapterExpr.Method.TargetObject = new CodeTypeReferenceExpression(typeof(System.AddIn.Pipeline.CollectionAdapters));
           
            
           
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

        internal static bool TypeNeedsAdapting(Type contractType)
        {
            return typeof(IContract).IsAssignableFrom(contractType) | contractType.IsEnum;
        }

        internal void BuildContractToViewAdapter(Type contractType, PipelineSegmentSource component, SegmentType componentType, bool activatable)
        {
            //Set up type
            String typeName = _symbols.GetNameFromType(contractType, componentType,SegmentDirection.ContractToView,false);
            SegmentType viewType;
            if (componentType == SegmentType.ASA)
            {
                viewType = SegmentType.AIB;
            }
            else
            {
                viewType = SegmentType.HAV;
            }
            String viewName = _symbols.GetNameFromType(contractType, viewType);
            CodeCompileUnit ccu = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(_symbols.GetNameSpace(componentType,contractType));
            CodeTypeDeclaration type = new CodeTypeDeclaration(typeName);
            type.TypeAttributes = TypeAttributes.Public;
            type.BaseTypes.Add(new CodeTypeReference(viewName));
            if (activatable)
            {
                CodeAttributeDeclaration marker = new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.AddIn.Pipeline.HostAdapterAttribute)));
                type.CustomAttributes.Add(marker);
            }
            CodeMemberField contract = new CodeMemberField(contractType, "_contract");
            CodeMemberField handle = new CodeMemberField(typeof(ContractHandle), "_handle");
            //AddDisposePattern(type, "_handle", "_contract");
            type.Members.Add(contract);
            type.Members.Add(handle);
            //Build constructor
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression(contractType, "contract");
            constructor.Parameters.Add(parameter);
            CodeAssignStatement assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_contract"), new CodeVariableReferenceExpression("contract"));
            constructor.Statements.Add(assign);
            CodeObjectCreateExpression createHandle = new CodeObjectCreateExpression(typeof(ContractHandle), new CodeVariableReferenceExpression("contract"));
            assign = new CodeAssignStatement(new CodeVariableReferenceExpression("_handle"), createHandle);
            constructor.Statements.Add(assign);
            type.Members.Add(constructor);
            SegmentType viewComponentType;
            switch (componentType)
            {
                case SegmentType.ASA:
                    viewComponentType = SegmentType.AIB;
                    break;
                case SegmentType.HSA:
                    viewComponentType = SegmentType.HAV;
                    break;
                default:
                    throw new InvalidOperationException("Must be asa or hsa");
            }
            if (IsEvent(contractType))
            {
                CodeMemberMethod handler = new CodeMemberMethod();
                handler.Name = "Handler";
                handler.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                CodeParameterDeclarationExpression sender = new CodeParameterDeclarationExpression(typeof(Object), "sender");
                MethodInfo mi = contractType.GetMethods()[0];
                
                ParameterInfo pi = mi.GetParameters()[0];
                CodeTypeReference eventArgsType = new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, viewType, SegmentDirection.None, true));              
                CodeParameterDeclarationExpression args = new CodeParameterDeclarationExpression(eventArgsType, "args");
                handler.Parameters.Add(sender);
                handler.Parameters.Add(args);
                handler.ReturnType = new CodeTypeReference(typeof(void));
                CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
                if (TypeNeedsAdapting(pi.ParameterType))
                {   
                    CodeMethodInvokeExpression argsAdapter = CallStaticAdapter(componentType, pi.ParameterType, new CodeVariableReferenceExpression("args"), SegmentDirection.ViewToContract);
                    cmi.Parameters.Add(argsAdapter);
                }
                else
                {
                    cmi.Parameters.Add(new CodeVariableReferenceExpression("args"));
                }
                cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_contract"), mi.Name);
                if (mi.ReturnType.Equals(typeof(bool)))
                {
                    CodeConditionStatement ifStatement = new CodeConditionStatement();
                    CodeAssignStatement assignCancel = new CodeAssignStatement();
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
                Dictionary<String, CodeMemberProperty> props = new Dictionary<string, CodeMemberProperty>();
                List<MethodInfo> setEvents = new List<MethodInfo>();
                foreach (MethodInfo mi in GetMethodsFromContract(contractType,true))
                {
                    
                    CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
                    if (IsEventAdd(mi))
                    {
                        PipelineHints.EventAddAttribute attr = GetEventAdd(mi);
                        //Add event to list for Static Constructor Initialization
                        setEvents.Add(mi);
                        //Hook up event during constructor
                        ParameterInfo pi = mi.GetParameters()[0];
                        CodeTypeReference adapterType =
                            new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, componentType, SegmentDirection.ViewToContract, false));
                        CodeObjectCreateExpression adapter = new CodeObjectCreateExpression(adapterType, new CodeThisReferenceExpression(),new CodeVariableReferenceExpression("s_" + mi.Name + "Fire"));
                        CodeMemberField handlerField = new CodeMemberField();
                        handlerField.Name = attr.Name + "_Handler";
                        handlerField.Type = adapterType;
                        type.Members.Add(handlerField);
                        CodeAssignStatement assignHandlerField = new CodeAssignStatement();
                        assignHandlerField.Left = new CodeVariableReferenceExpression(handlerField.Name);
                        assignHandlerField.Right = adapter;
                        constructor.Statements.Add(assignHandlerField);


                        //cmi = new CodeMethodInvokeExpression();
                        //cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("_contract"), mi.Name);
                        //cmi.Parameters.Add(adapter);
                        //constructor.Statements.Add(cmi);
                        
                        
                        //Add field
                        CodeMemberEvent eventField = new CodeMemberEvent();
                        eventField.Name = "_" + attr.Name;
                        eventField.Type = GetEventViewType(viewComponentType, mi, true);
                        type.Members.Add(eventField);

                        //Add FireMethod
                        CodeMemberMethod eventFire = new CodeMemberMethod();
                        eventFire.Attributes = MemberAttributes.Assembly;
                        eventFire.Name = "Fire" + eventField.Name;
                        eventFire.Parameters.Add(
                            new CodeParameterDeclarationExpression(eventField.Type.TypeArguments[0], "args"));
                        CodeMethodInvokeExpression eventFireInvoke = new CodeMethodInvokeExpression();
                        eventFireInvoke.Method = new CodeMethodReferenceExpression();
                        eventFireInvoke.Method.MethodName = "Invoke";
                        eventFireInvoke.Method.TargetObject = new CodeVariableReferenceExpression(eventField.Name);
                        eventFireInvoke.Parameters.Add(new CodeThisReferenceExpression());
                        eventFireInvoke.Parameters.Add(new CodeVariableReferenceExpression("args"));
                        CodeConditionStatement nullConditionalFire = new CodeConditionStatement();
                        CodeBinaryOperatorExpression eventNullCheck = new CodeBinaryOperatorExpression();
                        eventNullCheck.Left = new CodeVariableReferenceExpression(eventField.Name);
                        eventNullCheck.Right = new CodePrimitiveExpression(null);
                        eventNullCheck.Operator = CodeBinaryOperatorType.IdentityEquality;
                        nullConditionalFire.Condition = eventNullCheck;
                        nullConditionalFire.FalseStatements.Add(eventFireInvoke);
                        eventFire.Statements.Add(nullConditionalFire);
                        type.Members.Add(eventFire);

                        CodeSnippetTypeMember snippet = GetEventContractToViewFromSnippet(contractType,mi,eventField);
                        //Add override property;
                        type.Members.Add(snippet);
                       
                        continue;
                    }
                    else if (IsEventRemove(mi))
                    {
                        continue;
                    }
                    CodeMemberMethod method = new CodeMemberMethod();
                    CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
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

                    if (mi.ReturnType.Equals(typeof(void)))
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
                        if (IsIListContract(mi.ReturnType))
                        {
                            Type genericParamType = GetListGenericParamterType(mi.ReturnType);
                            method.ReturnType = GetIListContractTypeRef(viewType, SegmentDirection.ContractToView,  mi.ReturnType,mi.DeclaringType);
                            ret.Expression = CallListAdapter(SegmentDirection.ContractToView, componentType, viewType, cmi, genericParamType);
                        }
                        else
                        {
                            if (IsNativeHandle(mi.ReturnType))
                            {
                                method.ReturnType = GetNativeHandleViewType(SegmentDirection.ViewToContract);
                            }
                            else
                            {
                                method.ReturnType = new CodeTypeReference(_symbols.GetNameFromType(mi.ReturnType, viewComponentType, SegmentDirection.ContractToView));

                            }
                            CodeMethodInvokeExpression adaptExpr = CallStaticAdapter(componentType, mi.ReturnType, cmi, SegmentDirection.ContractToView);
                            ret.Expression = adaptExpr;
                        }
                        
                    }
                    foreach (ParameterInfo pi in mi.GetParameters())
                    {
                        if (!TypeNeedsAdapting(pi.ParameterType))
                        {
                            CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(pi.ParameterType, pi.Name);
                            method.Parameters.Add(cp);
                            cmi.Parameters.Add(new CodeVariableReferenceExpression(pi.Name));
                        }
                        else
                        {
                            CodeMethodInvokeExpression adaptExpr ;
                            if (IsIListContract(pi.ParameterType))
                            {
                                CodeTypeReference paramType = GetIListContractTypeRef(viewComponentType, SegmentDirection.ContractToView,  pi.ParameterType,mi.DeclaringType);
                                method.Parameters.Add(new CodeParameterDeclarationExpression(paramType, pi.Name));
                                Type genericParamType = GetListGenericParamterType(pi.ParameterType);
                                adaptExpr = CallListAdapter(SegmentDirection.ViewToContract, componentType, viewType, new CodeVariableReferenceExpression(pi.Name), genericParamType);
                            }
                            else
                            {
                                CodeTypeReference paramType ;
                                if (IsNativeHandle(pi.ParameterType))
                                {
                                    paramType = GetNativeHandleViewType(SegmentDirection.ContractToView);
                                }
                                else
                                {
                                    paramType = new CodeTypeReference(_symbols.GetNameFromType(pi.ParameterType, viewComponentType, SegmentDirection.None, true));
                                }
                                CodeParameterDeclarationExpression cp = new CodeParameterDeclarationExpression(paramType, pi.Name);
                                method.Parameters.Add(cp);
                                adaptExpr = CallStaticAdapter(componentType, pi.ParameterType, new CodeVariableReferenceExpression(pi.Name), SegmentDirection.ViewToContract);
                            }
                           
                            cmi.Parameters.Add(adaptExpr);
                        }
                    }
                    if (IsProperty(mi))
                    {
                        CodeMemberProperty myProp = GetProperyDecl(contractType, type, mi, props, viewComponentType, SegmentDirection.ContractToView, true);
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
                            method.Statements.Add(ret);
                        }
                        else
                        {
                            method.Statements.Add(cmi);
                        }

                        type.Members.Add(method);
                    }
                }
                ProcessSetEvents(viewName, type, setEvents);
            
            }
            CodeMemberMethod unadapt = new CodeMemberMethod();
            unadapt.Name = "GetSourceContract";
            unadapt.ReturnType = new CodeTypeReference(contractType);
            unadapt.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
            unadapt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("_contract")));
            type.Members.Add(unadapt);

            codeNamespace.Types.Add(type);
            ccu.Namespaces.Add(codeNamespace);
            component.Files.Add(new SourceFile(typeName,ccu));
        }



        private static CodeSnippetTypeMember GetEventContractToViewFromSnippet(Type t,MethodInfo mi,  CodeMemberEvent eventField)
        {
            PipelineHints.EventRemoveAttribute rAttr = null;
            PipelineHints.EventAddAttribute attr = GetEventAdd(mi);
            MethodInfo rMi = null;
            foreach (MethodInfo method in t.GetMethods())
            {
                if (IsEventRemove(method))
                {
                    if (GetEventRemove(method).Name.Equals(attr.Name))
                    {
                        rAttr = GetEventRemove(method);
                        rMi = method;
                        break;
                    }
                }
            }
            if (rAttr == null)
            {
                throw new InvalidOperationException("Can not find matching unsubscribe method");
            }

            String snippetText = "\t\tpublic event System.EventHandler<{0}>{4}{{\n\t\t\t";
            snippetText += "add{{\n\t\t\t\t";
            snippetText += "if ({2} == null)\n\t\t\t";
            snippetText += "{{\n\t\t\t\t";
            snippetText += "_contract.{1}({4}_Handler);\n\t\t\t";
            snippetText += "}}\n\t\t\t";
            snippetText += "{2} += value;\n\t\t\t";
            snippetText += "}}\n\t\t\t";
            snippetText += "remove{{\n\t\t\t\t";
            snippetText += "{2} -= value;\n\t\t\t";
            snippetText += "if ({2} == null)\n\t\t\t";
            snippetText += "{{\n\t\t\t\t";
            snippetText += "_contract.{3}({4}_Handler);\n\t\t\t";
            snippetText += "}}\n\t\t\t";
            snippetText += "}}\n\t\t}}";
            snippetText = String.Format(snippetText, eventField.Type.UserData["eventArgsTypeName"], mi.Name, eventField.Name,rMi.Name,attr.Name);
            CodeSnippetTypeMember snippet = new CodeSnippetTypeMember(snippetText);
            return snippet;
        }

        private void ProcessSetEvents(String viewName, CodeTypeDeclaration type, List<MethodInfo> events)
        {
            CodeTypeConstructor typeConstructor = new CodeTypeConstructor();
            typeConstructor.Attributes |= MemberAttributes.Private;
            CodeTypeOfExpression adapterType = new CodeTypeOfExpression(type.Name);
            foreach (MethodInfo mi in events)
            {
                PipelineHints.EventAddAttribute attr = GetEventAdd(mi);
                CodeMemberField field = new CodeMemberField();
                field.Name = "s_" + mi.Name + "Fire";
                field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
                field.Type = new CodeTypeReference(typeof(MethodInfo));
                CodeAssignStatement init = new CodeAssignStatement();
                CodeMethodInvokeExpression getEventFire = new CodeMethodInvokeExpression();
                getEventFire.Method = new CodeMethodReferenceExpression();
                getEventFire.Method.MethodName = "GetMethod";
                getEventFire.Method.TargetObject = adapterType;
                getEventFire.Parameters.Add(new CodePrimitiveExpression("Fire_" + attr.Name));
                CodeCastExpression bindingFlags =
                    new CodeCastExpression(new CodeTypeReference(
                        typeof(System.Reflection.BindingFlags)),
                        new CodePrimitiveExpression(
                            (int)(System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)));
                getEventFire.Parameters.Add(bindingFlags);
                init.Right = getEventFire;
                init.Left = new CodeVariableReferenceExpression(field.Name);
                typeConstructor.Statements.Add(init);
                type.Members.Add(field);
            }
            type.Members.Add(typeConstructor);
        }

        private void AddDisposePattern(CodeTypeDeclaration type, String handleName,String contractName)
        {
            CodeMemberField disposed = new CodeMemberField(typeof(bool), "_disposed");
            disposed.InitExpression = new CodePrimitiveExpression(false);
            type.BaseTypes.Add(typeof(IDisposable));
            CodeMemberMethod dispose = new CodeMemberMethod();
            dispose.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            dispose.Name = "Dispose";
            CodeConditionStatement ifNotDisposed = new CodeConditionStatement();
            ifNotDisposed.Condition = new CodeVariableReferenceExpression("_disposed");
            ifNotDisposed.FalseStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(handleName), "Dispose"));
            ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(contractName), new CodePrimitiveExpression(null)));
            ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(handleName), new CodePrimitiveExpression(null)));
            ifNotDisposed.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("_disposed"), new CodePrimitiveExpression(true)));
            ifNotDisposed.FalseStatements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(GC)), "SuppressFinalize",new CodeThisReferenceExpression()));
            dispose.Statements.Add(ifNotDisposed);
            type.Members.Add(dispose);
            type.Members.Add(disposed);
        }

        private CodeMethodInvokeExpression CallStaticAdapter(SegmentType componentType, Type contractType, CodeExpression cmi, SegmentDirection direction)
        {
            if (IsNativeHandle(contractType))
            {
                CodeMethodInvokeExpression adapterInvoke = new CodeMethodInvokeExpression();
                if (direction == SegmentDirection.ContractToView)
                {
                    adapterInvoke.Method = new CodeMethodReferenceExpression();
                    adapterInvoke.Method.MethodName = "ContractToViewAdapter";
                    adapterInvoke.Method.TargetObject = new CodeVariableReferenceExpression("System.AddIn.Pipeline.FrameworkElementAdapters");
                    adapterInvoke.Parameters.Add(cmi);
                }
                else
                {
                    adapterInvoke.Method = new CodeMethodReferenceExpression();
                    adapterInvoke.Method.MethodName = "ViewToContractAdapter";
                    adapterInvoke.Method.TargetObject = new CodeVariableReferenceExpression("System.AddIn.Pipeline.FrameworkElementAdapters");
                    adapterInvoke.Parameters.Add(cmi);
                }
                return adapterInvoke;
            }
            else
            {
                CodeMethodInvokeExpression adaptExpr = new CodeMethodInvokeExpression();
                CodeTypeReferenceExpression adapterType = new CodeTypeReferenceExpression(_symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, true));
                String adapterMethodName = _symbols.GetStaticAdapterMethodNameName(contractType, componentType, direction);
                CodeMethodReferenceExpression adaptMethod = new CodeMethodReferenceExpression(adapterType, adapterMethodName);
                adaptExpr.Method = adaptMethod;
                adaptExpr.Parameters.Add(cmi);
                return adaptExpr;
            }
        }

        private CodeMethodReferenceExpression GetStaticAdapter(SegmentType componentType, Type contractType, SegmentDirection direction)
        {      
            CodeTypeReferenceExpression adapterType = new CodeTypeReferenceExpression(_symbols.GetNameFromType(contractType, componentType, SegmentDirection.None, true));
            String adapterMethodName = _symbols.GetStaticAdapterMethodNameName(contractType, componentType, direction);
            return new CodeMethodReferenceExpression(adapterType, adapterMethodName);
        }
       
        private List<MethodInfo> GetMethodsFromContract(Type contract, bool inherit)
        {
            if (!typeof(IContract).IsAssignableFrom(contract))
            {
                throw new InvalidOperationException("Need an IContract type as input");
            }
            List<MethodInfo> methods = new List<MethodInfo>();
            methods.AddRange(contract.GetMethods());
            foreach (Type t in contract.GetInterfaces())
            {
                if (typeof(IContract).IsAssignableFrom(t) && !t.Equals(typeof(IContract)) && (inherit || !IsBaseType(contract,t)))
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

        private Type GetBaseContract(Type contract)
        {
            foreach (Type t in contract.GetInterfaces())
            {
                if (typeof(IContract).IsAssignableFrom(t) && !t.Equals(typeof(IContract)) && IsBaseType(contract, t))
                {
                    return t;
                }
            }
            return null;
        }

        private bool IsBaseType(Type main, Type sub)
        {
            Attribute[] attributes = (Attribute[])main.GetCustomAttributes(typeof(PipelineHints.BaseClassAttribute), false);
            foreach (Attribute attr in attributes)
            {
                PipelineHints.BaseClassAttribute baseClass = attr as PipelineHints.BaseClassAttribute;
                if (baseClass != null)
                {
                    if (baseClass.Base.Equals(sub) || sub.IsAssignableFrom(baseClass.Base))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

       

 
    }
}
