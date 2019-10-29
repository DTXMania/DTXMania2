using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2.old.RecordDBRecord
{
    class v002_RecordDBRecord
    {
        public const int VERSION = 2;

        /// <summary>
        ///		ユーザを一意に識別するID。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///		曲譜面ファイルのハッシュ値。
        /// </summary>
        public string SongHashId { get; set; }

        /// <summary>
        ///		スコア。
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///		カウントマップラインのデータ。
        ///		１ブロックを１文字（'0':0～'C':12）で表し、<see cref="DTXMania.ステージ.演奏.カウントマップライン.カウントマップの最大要素数"/> 個の文字が並ぶ。
        ///		もし不足分があれば、'0' とみなされる。
        /// </summary>
        public string CountMap { get; set; }

        /// <summary>
        ///		曲別SKILL。
        /// </summary>
        public double Skill { get; set; }

        /// <summary>
        ///		達成率。
        /// </summary>
        public double Achievement { get; set; }


        // 生成と終了

        public v002_RecordDBRecord()
        {
            this.UserId = "Anonymous";
            this.SongHashId = "";
            this.Score = 0;
            this.CountMap = "";
            this.Skill = 0.0;
            this.Achievement = 0.0;
        }
    }
}
