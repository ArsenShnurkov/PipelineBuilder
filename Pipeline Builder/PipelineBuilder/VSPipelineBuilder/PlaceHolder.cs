using EnvDTE;

namespace VSPipelineBuilder
{
	class PlaceHolder<T>
	{
		private readonly string _Name;
		private readonly Project _Project;

		public PlaceHolder(string fst, Project snd)
		{
			_Name = fst;
			_Project = snd;
		}

		public string Name
		{
			get { return _Name; }
		}

		public Project Project
		{
			get { return _Project; }
		}

		public override string ToString()
		{
			return Name;
		}
	}
}