﻿using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Plainion.GraphViz.Modules.Documents
{
    [Export( typeof( OpenDocumentsView ) )]
    public partial class OpenDocumentsView : UserControl
    {
        [ImportingConstructor]
        public OpenDocumentsView(OpenDocumentsViewModel model)
        {
            InitializeComponent();

            DataContext = model;
        }
    }
}
