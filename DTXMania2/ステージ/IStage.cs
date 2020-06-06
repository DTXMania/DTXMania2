using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    interface IStage : IDisposable
    {
        void 進行描画する();
    }
}
