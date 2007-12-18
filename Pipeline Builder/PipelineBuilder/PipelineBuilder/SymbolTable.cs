/// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace PipelineBuilder
{
    public class SymbolTable
    {
        String _rootName;
        bool _sharedView = false;
        Dictionary<SegmentType, String> _namespaceMapping;
        Dictionary<SegmentType, String> _assemblyNameMapping;
       
        internal SymbolTable(Assembly root)
        {
            _namespaceMapping = new Dictionary<SegmentType, string>();
            _assemblyNameMapping = new Dictionary<SegmentType, string>();
            _sharedView = root.GetCustomAttributes(typeof(PipelineHints.ShareViews), false).Length > 0;
            _rootName = root.GetName().Name;
            InitAssemblyNames(root);
            InitNamespace(root);
            
        }

        internal string GetRootNameSpace(SegmentType component)
        {
            return _namespaceMapping[component];
        }

        internal string GetNameSpace(SegmentType component, Type contractType)
        {
            object[] namespaceAttributes = contractType.GetCustomAttributes(typeof(PipelineHints.NamespaceAttribute), false);
            foreach (PipelineHints.NamespaceAttribute attr in namespaceAttributes)
            {
                if (ComponentsAreEquivalent(component, attr.Segment))
                {
                    return attr.Name;
                }
            }
            String contractNamespace = contractType.FullName.Remove(contractType.FullName.LastIndexOf("."));
            if (contractNamespace.EndsWith(".Contracts") && !(component.Equals(SegmentType.ASA) || component.Equals(SegmentType.HSA)))
            {
                return contractNamespace.Remove(contractNamespace.LastIndexOf("."));
            }
            return _namespaceMapping[component];
        }

        internal static bool ComponentsAreEquivalent(SegmentType component, PipelineHints.PipelineSegment pipelineHints)
        {
            if (component.Equals(SegmentType.ASA) && pipelineHints.Equals(PipelineHints.PipelineSegment.AddInSideAdapter))
            {
                return true;
            }
            if (component.Equals(SegmentType.HSA) && pipelineHints.Equals(PipelineHints.PipelineSegment.HostSideAdapter))
            {
                return true;
            }
            if (component.Equals(SegmentType.HAV) && pipelineHints.Equals(PipelineHints.PipelineSegment.HostView))
            {
                return true;
            }
            if (component.Equals(SegmentType.AIB) && pipelineHints.Equals(PipelineHints.PipelineSegment.AddInView))
            {
                return true;
            }
            if ((pipelineHints.Equals(PipelineHints.PipelineSegment.Views) &&
                    (component.Equals(SegmentType.HAV) || component.Equals(SegmentType.AIB) || component.Equals(SegmentType.VIEW))))
            {
                return true;
            }
            return false;
           
        }

        internal void InitAssemblyNames(Assembly asm)
        {
            _assemblyNameMapping[SegmentType.HAV] = "HostView";
            _assemblyNameMapping[SegmentType.HSA] = "HostSideAdapters";
            _assemblyNameMapping[SegmentType.ASA] = "AddInSideAdapters";
            _assemblyNameMapping[SegmentType.AIB] = "AddInView";
            _assemblyNameMapping[SegmentType.VIEW] = "View";
            foreach (PipelineHints.SegmentAssemblyNameAttribute attr in asm.GetCustomAttributes(typeof(PipelineHints.SegmentAssemblyNameAttribute), false))
            {
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostView))
                {
                    _assemblyNameMapping[SegmentType.HAV] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInView))
                {
                    _assemblyNameMapping[SegmentType.AIB] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostSideAdapter))
                {
                    _assemblyNameMapping[SegmentType.HSA] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInSideAdapter))
                {
                    _assemblyNameMapping[SegmentType.ASA] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.Views))
                {
                    _assemblyNameMapping[SegmentType.VIEW] = attr.Name;
                }
            }
        }

        internal String GetAssemblyName(SegmentType component)
        {
            return _assemblyNameMapping[component];
        }

        internal void InitNamespace(Assembly asm)
        {
            String contractAssemblyName = _rootName;
            if (contractAssemblyName.EndsWith(".Contracts"))
            {
                contractAssemblyName = contractAssemblyName.Remove(contractAssemblyName.LastIndexOf(".Contracts"));
            }
            _namespaceMapping[SegmentType.HAV] = contractAssemblyName;
            _namespaceMapping[SegmentType.HSA]=  contractAssemblyName + "HostAdapers";
            _namespaceMapping[SegmentType.ASA] = contractAssemblyName + "AddInAdapters";
            _namespaceMapping[SegmentType.AIB] = contractAssemblyName;
            _namespaceMapping[SegmentType.VIEW] = contractAssemblyName;
            foreach (PipelineHints.NamespaceAttribute attr in asm.GetCustomAttributes(typeof(PipelineHints.NamespaceAttribute), false))
            {
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostView))
                {
                    _namespaceMapping[SegmentType.HAV] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInView))
                {
                    _namespaceMapping[SegmentType.AIB] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.AddInSideAdapter))
                {
                    _namespaceMapping[SegmentType.ASA] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.HostSideAdapter))
                {
                    _namespaceMapping[SegmentType.HSA] = attr.Name;
                }
                if (attr.Segment.Equals(PipelineHints.PipelineSegment.Views))
                {
                    _namespaceMapping[SegmentType.VIEW] = attr.Name;
                }
            }

           
        }

        private string NormalizeContractName(String name)
        {
            String result = name;
            if (name.Equals("IContract"))
            {
                return name;
            }
            if (result.StartsWith("I"))
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
            if (!PipelineBuilder.TypeNeedsAdapting(type))
            {
                return type.FullName;
            }
            if (type.Equals(typeof(System.AddIn.Contract.INativeHandleContract)))
            {
                if (segmentType.Equals(SegmentType.VIEW) || segmentType.Equals(SegmentType.AIB) || segmentType.Equals(SegmentType.HAV))
                {
                    return typeof(System.Windows.FrameworkElement).FullName;
                }
                else
                {
                    return typeof(System.AddIn.Pipeline.FrameworkElementAdapters).FullName;
                }
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
            String typeName = NormalizeContractName(type.Name);
            if (PipelineBuilder.IsViewInterface(type))
            {
                typeName = "I" + typeName;
            }
            if (prefix)
            {
                if (_sharedView && (segmentType.Equals(SegmentType.HAV) || segmentType.Equals(SegmentType.AIB) || segmentType.Equals(SegmentType.VIEW)))
                {
                    refPrefix = GetNameSpace(SegmentType.VIEW,type) + ".";
                }
                else
                {
                    refPrefix = GetNameSpace(segmentType,type) + ".";
                }
            }
            switch (segmentType)
            {
                case SegmentType.HAV:
                    return refPrefix + typeName;
                case SegmentType.HSA:
                    return refPrefix + typeName + refSuffix + "HostAdapter";
                case SegmentType.ASA:
                    return refPrefix + typeName + refSuffix + "AddInAdapter";
                case SegmentType.AIB:
                    return refPrefix + typeName;
                case SegmentType.VIEW:
                    return refPrefix + typeName;
                default:
                    throw new InvalidOperationException("No segment type specified: " + segmentType);
            }
        }

    }
}
