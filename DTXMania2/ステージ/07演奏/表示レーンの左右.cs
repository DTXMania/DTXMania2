using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DTXMania2.演奏
{
    [DataContract( Name = "LanePosition", Namespace = "" )]
    struct 表示レーンの左右
    {
        /// <summary>
        ///		演奏画面で、Ride/Ride_Cupチップを左シンバルレーン上に表示するなら true、
        ///		右シンバルレーン上に表示するなら false。
        /// </summary>
        [DataMember]
        public bool Rideは左 { get; set; }

        /// <summary>
        ///		演奏画面で、Chinaチップを左シンバルレーン上に表示するなら true、
        ///		右シンバルレーン上に表示するなら false。
        /// </summary>
        [DataMember]
        public bool Chinaは左 { get; set; }

        /// <summary>
        ///		演奏画面で、Splashチップを左シンバルレーン上に表示するなら true、
        ///		右シンバルレーン上に表示するなら false。
        /// </summary>
        [DataMember]
        public bool Splashは左 { get; set; }
    }
}
