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
    
    
    public class IDocumentOpenedHandlerViewToContractHostAdapter : System.AddIn.Pipeline.ContractBase, PipelineBuilder.Samples.Events.Contracts.IDocumentOpenedHandlerContract {
        
        private object _view;
        
        private System.Reflection.MethodInfo _event;
        
        public IDocumentOpenedHandlerViewToContractHostAdapter(object view, System.Reflection.MethodInfo eventProp) {
            _view = view;
            _event = eventProp;
        }
        
        public void Handle(PipelineBuilder.Samples.Events.Contracts.IDocumentOpenedEventArgsContract args) {
            DocumentOpenedEventArgsContractToViewHostAdapter adaptedArgs;
            adaptedArgs = new DocumentOpenedEventArgsContractToViewHostAdapter(args);
            object[] argsArray = new object[1];
            argsArray[0] = adaptedArgs;
            _event.Invoke(_view, argsArray);
        }
        
        internal object GetSourceView() {
            return _view;
        }
    }
}

