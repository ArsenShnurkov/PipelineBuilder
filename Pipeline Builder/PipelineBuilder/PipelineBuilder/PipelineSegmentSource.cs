/// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;

namespace PipelineBuilder
{
	[Serializable]
	public class PipelineSegmentSource
	{
		private readonly List<SourceFile> _files;
		private readonly string _name;
		private readonly SegmentType _type;

		public PipelineSegmentSource(SegmentType type, SymbolTable symbols)
		{
			_type = type;
			_name = symbols.GetAssemblyName(type);
			_files = new List<SourceFile>();
		}

		public SegmentType Type
		{
			get { return _type; }
		}

		public List<SourceFile> Files
		{
			get { return _files; }
		}

		public string Name
		{
			get { return _name; }
		}
	}

	[Flags]
	public enum SegmentType
	{
		HostAddInView = 0,
		HostSideAdapter = 1,
		AddInSideAdapter = 2,
		AddInView = 4,
		View = 8
	}

	public enum SegmentDirection
	{
		ViewToContract,
		ContractToView,
		None
	}
}