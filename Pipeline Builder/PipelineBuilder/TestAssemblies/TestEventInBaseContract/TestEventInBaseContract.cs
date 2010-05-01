using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using PipelineHints;

namespace Contracts
{
    [System.AddIn.Pipeline.AddInContract]
    public interface IAddInContract : IContract
    {
        [EventAdd( "WorkProgress" )]
        void WorkProgressEventAdd( IWorkProgressEventHandler handler );
        [EventRemove( "WorkProgress" )]
        void WorkProgressEventRemove( IWorkProgressEventHandler handler );
    }

    public interface IDerivedContract : IAddInContract
    {

    }

    #region Events fired by the add-in

    [EventHandler]
    public interface IWorkProgressEventHandler : IContract
    {
        bool Handler( IWorkProgressEventArgs args );
    }

    [EventArgs( Cancelable = true )]
    public interface IWorkProgressEventArgs : IContract
    {
        double PercentComplete
        {
            get;
        }
    }

    #endregion
}
