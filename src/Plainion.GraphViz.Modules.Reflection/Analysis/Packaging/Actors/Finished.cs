﻿using System;
using Akka.Actor;

namespace Plainion.GraphViz.Modules.Reflection.Analysis.Packaging.Actors
{
    class Finished
    {
        private readonly IActorRef mySender;

        public Finished( IActorRef sender )
        {
            mySender = sender;
        }

        public IActorRef Sender
        {
            get { return mySender; }
        }

        public string ResponseFile { get; set; }

        public Exception Exception { get; set; }
    }
}
