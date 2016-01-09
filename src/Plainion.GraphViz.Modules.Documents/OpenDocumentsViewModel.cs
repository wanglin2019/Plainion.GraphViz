﻿using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Plainion.GraphViz.Infrastructure.Services;
using Plainion.GraphViz.Infrastructure.ViewModel;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Plainion.Prism.Interactivity.InteractionRequest;

namespace Plainion.GraphViz.Modules.Documents
{
    [Export( typeof( OpenDocumentsViewModel ) )]
    public class OpenDocumentsViewModel : ViewModelBase
    {
        [Import]
        internal IPresentationCreationService PresentationCreationService { get; set; }

        [Import]
        public IStatusMessageService StatusMessageService { get; set; }

        public OpenDocumentsViewModel()
        {
            OpenDocumentCommand = new DelegateCommand( OpenDocument );
            OpenFileRequest = new InteractionRequest<OpenFileDialogNotification>();
        }

        public DelegateCommand OpenDocumentCommand { get; private set; }

        public InteractionRequest<OpenFileDialogNotification> OpenFileRequest { get; private set; }
        
        private void OpenDocument()
        {
            var notification = new OpenFileDialogNotification();
            notification.RestoreDirectory = true;
            notification.Filter = "DGML files (*.dgml)|*.dgml|DOT files (*.dot)|*.dot|GraphML files (*.graphml)|*.graphml|DOT plain files (*.plain)|*.plain";
            notification.FilterIndex = 0;
            notification.DefaultExt = ".dgml";

            OpenFileRequest.Raise( notification,
                n =>
                {
                    if( n.Confirmed )
                    {
                        Open( n.FileName );
                    }
                } );
        }

        private void Open( string path )
        {
            var presentation = PresentationCreationService.CreatePresentation( Path.GetDirectoryName( path ) );

            var processor = new BasicDocumentProcessor( presentation );
            //processor.DocumentCreators[ ".dot" ] = () => new DotLangDocument( new DotToDotPlainConverter( myConfig.DotToolsHome ) );
            processor.DocumentCreators[ ".dot" ] = () => new DotLangPureDocument();

            processor.Process( path );

            if ( processor.FailedItems.Any() )
            {
                var sb = new StringBuilder();
                sb.AppendLine( "Following items could not be loaded successfully:" );
                sb.AppendLine();
                foreach ( var item in processor.FailedItems )
                {
                    sb.AppendLine( string.Format( "{0}: {1}", item.FailureReason, item.Item ) );
                }
                StatusMessageService.Publish( new StatusMessage( sb.ToString() ) );
            }

            Model.Presentation = presentation;
        }

        protected override void OnModelPropertyChanged( string propertyName )
        {
        }
    }
}