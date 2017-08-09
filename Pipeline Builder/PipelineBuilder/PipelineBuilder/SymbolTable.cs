/// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PipelineBuilder
{
    public class SymbolTable
    {
        String _rootName;
        Assembly _rootAssembly;
        bool _sharedView = false;
        Dictionary<SegmentType, String> _assemblyNameMapping;
       
        internal SymbolTable(Assembly root)
        {
            _rootAssembly = root;
            _assemblyNameMapping = new Dictionary<SegmentType, string>();
            _sharedView = root.GetCustomAttributes(typeof(PipelineHints.ShareViews), false).Length > 0;
            _rootName = root.GetName().Name;
            InitAssemblyNames(root);
        }

    
        internal string GetNameSpace(SegmentType component, Type contractType)
        {
            if (contractType.IsArray)
            {
                contractType = contractType.GetElementType();
            }
            object[] namespaceAttributes = contractType.GetCustomAttributes(typeof(PipelineHints.NamespaceAttribute), false);
            foreach (PipelineHints.NamespaceAttribute attr in namespaceAttributes)
            {
                if (ComponentsAreEquivalent(component, attr.Segment))
                {
                    return attr.Name;
                }
            }
            String contractNamespace = contractType.FullName.Remove(contractType.FullName.LastIndexOf("."));
            if (contractNamespace.EndsWith(".Contracts") || contractNamespace.EndsWith(".Contract"))
            {
                string viewNamespace = contractNamespace.Remove(contractNamespace.LastIndexOf("."));
                if (!(component.Equals(SegmentType.AddInSideAdapter) || component.Equals(SegmentType.HostSideAdapter)))
                {
                    return viewNamespace;
                }
                else if (component.Equals(SegmentType.AddInSideAdapter))
                {
                    return viewNamespace + ".AddInSideAdapters";
                }
                else if (component.Equals(SegmentType.HostSideAdapter))
                {
                    return viewNamespace + ".HostSideAdapters";
                }
            }
            else
            {
                switch (component)
                {
                    case SegmentType.AddInView:
                        return contractNamespace + ".AddInViews";
                    case SegmentType.AddInSideAdapter:
                        return contractNamespace + ".AddInSideAdapters";
                    case SegmentType.HostAddInView:
                        return contractNamespace + ".HostViews";
                    case SegmentType.HostSideAdapter:
                        return contractNamespace + ".HostSideAdapters";
                    case SegmentType.View:
                        return contractNamespace + ".Views";
                    default:
                        throw new InvalidOperationException("Component is not a valid type: " + component + "/" + contractType.FullName);
                }
            }
            throw new InvalidOperationException("Component is not a valid type: " + component + "/" + contractType.FullName);
            
        }

        internal static bool ComponentsAreEquivalent(SegmentType component, PipelineHints.PipelineSegment pipelineHints)
        {
            if (component.Equals(SegmentType.AddInSideAdapter) && pipelineHints.Equals(PipelineHints.PipelineSegment.AddInSideAdapter))
            {
                return true;
            }
            if (component.Equals(SegmentType.HostSideAdapter) && pipelineHints.Equals(PipelineHints.PipelineSegment.HostSideAdapter))
            {
                return true;
            }
            if (component.Equals(SegmentType.HostAddInView) && pipelineHints.Equals(PipelineHints.PipelineSegment.HostView))
            {
                return true;
            }
            if (component.Equals(SegmentType.AddInView) && pipelineHints.Equals(PipelineHints.PipelineSegment.AddInView))
            {
                return true;
            }
            if ((pipelineHints.Equals(PipelineHints.PipelineSegment.Views) &&
                    (component.Equals(SegmentType.HostAddInView) || component.Equals(SegmentType.AddInView) || component.Equals(SegmentType.View))))
            {
                return true;
            }
            return false;
           
        }

        internal void InitAssemblyNames(Assembly asm)
        {
            _assemblyNameMapping[SegmentType.HostAddInView] = "HostView";
            _assemblyNameMapping[SegmentType.HostSideAdapter] = "HostSideAdapters";
            _assemblyNameMapping[SegmentType.AddInSideAdapter] = "AddInSideAdapters";
            _assemblyNameMapping[SegmentType.AddInView] = "AddInView";
            _assemblyNameMapping[SegmentType.View] = "View";
            foreach (PipelineHints.SegmentAssemblyNameAttribute attr in asm.GetCustomAttributes(typeof(PipelineHints.SegmentAssemblyNameAttribute), false))
            {
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostView))
                {
                    _assemblyNameMapping[SegmentType.HostAddInView] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInView))
                {
                    _assemblyNameMapping[SegmentType.AddInView] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostSideAdapter))
                {
                    _assemblyNameMapping[SegmentType.HostSideAdapter] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInSideAdapter))
                {
                    _assemblyNameMapping[SegmentType.AddInSideAdapter] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.Views))
                {
                    _assemblyNameMapping[SegmentType.View] = attr.Name;
                }
            }
        }

        internal String GetAssemblyName(SegmentType component)
        {
            return _assemblyNameMapping[component];
        }

      

        private string NormalizeContractName(Type contract)
        {
            String name = contract.Name;
            String result = contract.Name;
            result = result.Replace("[]", "");
            if (name.Equals("IContract"))
            {
                return name;
            }
            if (result.StartsWith("I") && contract.IsInterface)
            {
                result = result.Substring(1);
            }
            if (result.EndsWith("Contract"))
            {
                result = result.Remove(result.LastIndexOf("Contract"));
            }
            return result;
        }

        internal string GetStaticAdapterMethodNameName(Type type, SegmentType component, SegmentDirection direction)
        {
           switch (direction)
            {
                case SegmentDirection.ContractToView:
                    return "ContractToViewAdapter";
                case SegmentDirection.ViewToContract:
                    return "ViewToContractAdapter";
                default: 
                    throw new InvalidOperationException("Must be either incoming our outgoing");
            }
        }

        internal string GetNameFromType(Type type, SegmentType component)
        {
            return GetNameFromType(type, component, SegmentDirection.None);
        }
        
        internal string GetNameFromType(Type type, SegmentType component,SegmentDirection direction)
        {
            return GetNameFromType(type,component,direction,true);
        }

        internal string GetNameFromType(Type type, SegmentType component, SegmentDirection direction, Type referenceType)
        {
            if (direction.Equals(SegmentDirection.None))
            {
                return GetNameFromType(type, component, direction,
                    !GetNameSpace(component, type).Equals(GetNameSpace(component, referenceType)));
            }
            else 
            {
                return GetNameFromType(type, component, direction, true);
            }
        }

        internal string GetNameFromType(Type type, SegmentType segmentType,SegmentDirection direction, bool prefix)
        {
            Type underlyingType;
            if (type.IsArray)
            {
                underlyingType = type.GetElementType();
            }
            else
            {
                underlyingType = type;
            }
            if (type.Equals(typeof(System.AddIn.Contract.INativeHandleContract)))
            {
                if (segmentType.Equals(SegmentType.View) || segmentType.Equals(SegmentType.AddInView) || segmentType.Equals(SegmentType.HostAddInView))
                {
                    return typeof(System.Windows.FrameworkElement).FullName;
                }
                else
                {
                    return typeof(System.AddIn.Pipeline.FrameworkElementAdapters).FullName;
                }
            }
            if (!type.Assembly.Equals(this._rootAssembly)) 
            {
                return type.FullName;
            }
            
            String refPrefix = "";
            String refSuffix = "";
            if (direction == SegmentDirection.ContractToView)
            {
                refSuffix = "ContractToView";
            }
            else if (direction == SegmentDirection.ViewToContract)
            {
                refSuffix = "ViewToContract";
            }
            String typeName = NormalizeContractName(type);
            if (PipelineBuilder.IsViewInterface(type))
            {
                typeName = "I" + typeName;
            }
            if (prefix)
            {
                if (_sharedView && (segmentType.Equals(SegmentType.HostAddInView) || segmentType.Equals(SegmentType.AddInView) || segmentType.Equals(SegmentType.View)))
                {
                    refPrefix = GetNameSpace(SegmentType.View,underlyingType) + ".";
                }
                else
                {
                    refPrefix = GetNameSpace(segmentType, underlyingType) + ".";
                }
            }
            if (type.IsArray)
            {
                if (segmentType.Equals(SegmentType.AddInSideAdapter) || segmentType.Equals(SegmentType.HostSideAdapter))
                {
                    typeName += "Array";
                }
                else
                {
                    typeName += "[]";
                }
            }
            switch (segmentType) 
            {
                case SegmentType.HostAddInView:
                    return refPrefix + typeName;
                case SegmentType.HostSideAdapter:
                    return refPrefix + typeName + refSuffix + "HostAdapter";
                case SegmentType.AddInSideAdapter:
                    return refPrefix + typeName + refSuffix + "AddInAdapter";
                case SegmentType.AddInView:
                    return refPrefix + typeName;
                case SegmentType.View:
                    return refPrefix + typeName;
                default:
                    throw new InvalidOperationException("No segment type specified: " + segmentType);
            }
        }

    }
}
