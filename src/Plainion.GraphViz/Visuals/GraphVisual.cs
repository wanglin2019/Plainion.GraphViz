using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Plainion.GraphViz.Model;
using Plainion.GraphViz.Presentation;
using System.Windows.Media.Effects;
using Plainion;

namespace Plainion.GraphViz.Visuals
{
    public class GraphVisual : FrameworkElement, IVisualPicking
    {
        private IGraphPresentation myPresentation;
        private DrawingVisual myDrawing;
        // represents the internal model of visuals
        // TODO: should we keep all elements here or only the visible ones?
        private IDictionary<string, AbstractElementVisual> myDrawingElements;

        private IModuleChangedJournal<Selection> mySelectionJournal;
        private IModuleChangedJournal<INodeMask> myNodeMaskJournal;
        private IModuleChangedJournal<Edge> myEdgeMaskJournal;

        public GraphVisual()
        {
            myDrawing = new DrawingVisual();
            myDrawingElements = new Dictionary<string, AbstractElementVisual>();

            ClipToBounds = true;

            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
        }

        public IGraphPresentation Presentation
        {
            get
            {
                return myPresentation;
            }
            set
            {
                if( myPresentation == value )
                {
                    return;
                }

                if( myPresentation != null )
                {
                    mySelectionJournal.Dispose();
                    myNodeMaskJournal.Dispose();
                    myEdgeMaskJournal.Dispose();

                    myDrawingElements.Clear();
                }

                myPresentation = value;

                if( myPresentation != null )
                {
                    mySelectionJournal = myPresentation.GetPropertySetFor<Selection>().CreateJournal();
                    myNodeMaskJournal = myPresentation.GetModule<INodeMaskModule>().CreateJournal();
                    myEdgeMaskJournal = myPresentation.GetModule<IEdgeMaskModule>().CreateJournal();
                }
            }
        }

        public event EventHandler RenderingFinished;

        public ILayoutEngine LayoutEngine
        {
            get;
            set;
        }

        public void Refresh()
        {
            if( Presentation == null )
            {
                return;
            }

            var layoutModule = Presentation.GetModule<IGraphLayoutModule>();

            // current assumption: it is enough to check the nodes as we would not render edges independent from nodes
            var reLayout = Presentation.Graph.Nodes
                .Where( node => Presentation.Picking.Pick( node ) )
                .Any( node => layoutModule.GetLayout( node ) == null );

            if( reLayout )
            {
                Contract.Invariant( LayoutEngine != null, "LayoutEngine not set" );

                Debug.WriteLine( "Relayouting" );
                LayoutEngine.Relayout( Presentation );

                // reLayout requires reRender
                myDrawingElements.Clear();
            }

            if( myDrawingElements.Count == 0 )
            {
                Debug.WriteLine( "Rerendering" );

                RemoveVisualChild( myDrawing );

                myDrawing.Children.Clear();

                myDrawing = new DrawingVisual();

                foreach( var node in Presentation.Graph.Nodes )
                {
                    var visual = new NodeVisual( node, Presentation );

                    myDrawingElements.Add( node.Id, visual );

                    if( Presentation.Picking.Pick( node ) )
                    {
                        var layoutState = layoutModule.GetLayout( node );
                        visual.Draw( layoutState );

                        myDrawing.Children.Add( visual.Visual );
                    }
                }

                foreach( var edge in Presentation.Graph.Edges )
                {
                    var visual = new EdgeVisual( edge, Presentation );

                    myDrawingElements.Add( edge.Id, visual );

                    if( Presentation.Picking.Pick( edge ) )
                    {
                        var layoutState = layoutModule.GetLayout( edge );
                        visual.Draw( layoutState );

                        myDrawing.Children.Add( visual.Visual );
                    }
                }

                AddVisualChild( myDrawing );

                var selectionModule = Presentation.GetPropertySetFor<Selection>();
                foreach( var e in selectionModule.Items )
                {
                    myDrawingElements[ e.OwnerId ].Select( e.IsSelected );
                }

                // clear journals - to avoid considering out-dated infos on next refresh
                myEdgeMaskJournal.Clear();
                myNodeMaskJournal.Clear();
                mySelectionJournal.Clear();

                InvalidateMeasure();

                if( RenderingFinished != null )
                {
                    RenderingFinished( this, EventArgs.Empty );
                }
            }
            else
            {
                if( !myNodeMaskJournal.IsEmpty )
                {
                    foreach( var node in Presentation.Graph.Nodes )
                    {
                        SetVisibility( ( NodeVisual )myDrawingElements[ node.Id ],
                            Presentation.Picking.Pick( node ),
                            v => v.Draw( layoutModule.GetLayout( v.Owner ) ) );
                    }

                    foreach( var edge in Presentation.Graph.Edges )
                    {
                        SetVisibility( ( EdgeVisual )myDrawingElements[ edge.Id ],
                            Presentation.Picking.Pick( edge ),
                            v => v.Draw( layoutModule.GetLayout( v.Owner ) ) );
                    }

                    myNodeMaskJournal.Clear();
                    InvalidateVisual();
                }

                if( !myEdgeMaskJournal.IsEmpty )
                {
                    // EdgeMask module can only hide additional edges so no picking required here
                    foreach( var edge in myEdgeMaskJournal.Entries )
                    {
                        SetVisibility( ( EdgeVisual )myDrawingElements[ edge.Id ], false, null );
                    }

                    myEdgeMaskJournal.Clear();
                    InvalidateVisual();
                }

                if( !mySelectionJournal.IsEmpty )
                {
                    foreach( var e in mySelectionJournal.Entries )
                    {
                        myDrawingElements[ e.OwnerId ].Select( e.IsSelected );
                    }

                    mySelectionJournal.Clear();
                    InvalidateVisual();
                }
            }
        }

        // when modifying the Children collection we have to consider that we could get here because of IsDirty
        // but IsVisible was already toggled multiple times so that actually nothing has to be done
        private void SetVisibility<T>( T visual, bool isVisible, Action<T> drawAction ) where T : AbstractElementVisual
        {
            if( isVisible )
            {
                if( visual.Visual == null )
                {
                    drawAction( visual );

                    myDrawing.Children.Add( visual.Visual );
                }
                else
                {
                    if( !myDrawing.Children.Contains( visual.Visual ) )
                    {
                        myDrawing.Children.Add( visual.Visual );
                    }
                }
            }
            else
            {
                if( myDrawing.Children.Contains( visual.Visual ) )
                {
                    myDrawing.Children.Remove( visual.Visual );
                }
            }
        }

        // TODO: do we really want to allow direct access?
        internal DrawingVisual Drawing
        {
            get
            {
                return myDrawing;
            }
        }

        // Provide a required override for the VisualChildCount property.
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild( int index )
        {
            return myDrawing;
        }

        public Size ContentSize
        {
            get
            {
                var bounds = myDrawing.ContentBounds;
                bounds.Union( myDrawing.DescendantBounds );

                return new Size( bounds.Width * 64, bounds.Height * 64 );
            }
        }

        protected override Size MeasureOverride( Size availableSize )
        {
            Rect bounds = myDrawing.ContentBounds;
            bounds.Union( myDrawing.DescendantBounds );

            if( bounds.IsEmpty )
            {
                // if the graph is empty
                return new Size( 8, 8 );
            }

            // add some extra padding in case the bezier curve is to huge
            bounds.Inflate( bounds.Width * 0.01, bounds.Height * 0.01 );

            var m = new Matrix();
            m.Translate( -bounds.Left, -bounds.Top );
            // TODO: why 64?
            m.Scale( 64, 64 );

            myDrawing.Transform = new MatrixTransform( m );

            return new Size( bounds.Width * 64, bounds.Height * 64 );
        }

        public Rect? GetBoundingBox( IGraphItem item )
        {
            if( myDrawingElements[ item.Id ].Visual == null )
            {
                return null;
            }

            var bounds = myDrawingElements[ item.Id ].Visual.ContentBounds;
            bounds.Scale( 64, 64 );
            return bounds;
        }

        public IGraphItem PickMousePosition()
        {
            return Pick( Mouse.GetPosition( this ) );
        }

        public IGraphItem Pick( Point position )
        {
            var result = VisualTreeHelper.HitTest( this, position );
            if( result == null )
            {
                return null;
            }

            var visual = result.VisualHit as DrawingVisual;

            if( visual == null )
            {
                return null;
            }

            return ( IGraphItem )visual.ReadLocalValue( AbstractElementVisual.GraphItemProperty );
        }
    }
}