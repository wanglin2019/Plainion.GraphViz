﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Plainion.GraphViz.Model;

namespace Plainion.GraphViz.Presentation
{
    class TransformationModule : AbstractModule<IGraphTransformation>, ITransformationModule
    {
        private GraphPresentation myPresentation;
        private IGraph myGraph;
        private List<IGraphTransformation> myTransformations;

        public TransformationModule( GraphPresentation presentation )
        {
            myPresentation = presentation;
            myTransformations = new List<IGraphTransformation>();
        }

        public IGraph Graph
        {
            get { return myTransformations.Count == 0 ? myPresentation.Graph : myGraph; }
        }

        public override IEnumerable<IGraphTransformation> Items
        {
            get { return myTransformations; }
        }

        public void Add( IGraphTransformation transformation )
        {
            myTransformations.Add( transformation );

            var notifyPropertyChanged = transformation as INotifyPropertyChanged;
            if( notifyPropertyChanged != null )
            {
                notifyPropertyChanged.PropertyChanged += OnTransformationChanged;
            }

            ApplyTransformations();

            RaiseCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Add, transformation ) );
        }

        private void OnTransformationChanged( object sender, PropertyChangedEventArgs e )
        {
            ApplyTransformations();
        }

        private void ApplyTransformations()
        {
            myGraph = myPresentation.Graph;

            foreach( var transformation in myTransformations )
            {
                myGraph = transformation.Transform( myGraph );
            }
        }

        public void Remove( IGraphTransformation transformation )
        {
            myTransformations.Remove( transformation );

            var notifyPropertyChanged = transformation as INotifyPropertyChanged;
            if( notifyPropertyChanged != null )
            {
                notifyPropertyChanged.PropertyChanged -= OnTransformationChanged;
            }

            ApplyTransformations();

            RaiseCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Remove, transformation ) );
        }
    }
}