//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PipelineBuilder.Samples.EventsHostAdapers {
    
    
    public class IAppObjectHostAdapter {
        
        internal static PipelineBuilder.Samples.Events.IAppObject ContractToViewAdapter(PipelineBuilder.Samples.Events.Contracts.IAppObjectContract contract) {
            if (((System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) != true) 
                        && contract.GetType().Equals(typeof(IAppObjectViewToContractHostAdapter)))) {
                return ((IAppObjectViewToContractHostAdapter)(contract)).GetSourceView();
            }
            else {
                return new IAppObjectContractToViewHostAdapter(contract);
            }
        }
        
        internal static PipelineBuilder.Samples.Events.Contracts.IAppObjectContract ViewToContractAdapter(PipelineBuilder.Samples.Events.IAppObject view) {
            if (view.GetType().Equals(typeof(IAppObjectContractToViewHostAdapter))) {
                return ((IAppObjectContractToViewHostAdapter)(view)).GetSourceContract();
            }
            else {
                return new IAppObjectViewToContractHostAdapter(view);
            }
        }
    }
}

