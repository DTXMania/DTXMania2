using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    interface IStage : IDisposable
    {
        void 進行する();

        void 描画する();
    }
}
