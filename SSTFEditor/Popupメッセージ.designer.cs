namespace SSTFEditor
{
	partial class Popupメッセージ
	{
		/// <summary>
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows フォーム デザイナで生成されたコード

		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( Popupメッセージ ) );
			this.pictureBoxイラスト = new System.Windows.Forms.PictureBox();
			this.panelメッセージ本文 = new System.Windows.Forms.Panel();
			( (System.ComponentModel.ISupportInitialize) ( this.pictureBoxイラスト ) ).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBoxイラスト
			// 
			this.pictureBoxイラスト.Image = global::SSTFEditor.Properties.Resources.りらちょー;
			resources.ApplyResources( this.pictureBoxイラスト, "pictureBoxイラスト" );
			this.pictureBoxイラスト.Name = "pictureBoxイラスト";
			this.pictureBoxイラスト.TabStop = false;
			// 
			// panelメッセージ本文
			// 
			resources.ApplyResources( this.panelメッセージ本文, "panelメッセージ本文" );
			this.panelメッセージ本文.Name = "panelメッセージ本文";
			this.panelメッセージ本文.Paint += new System.Windows.Forms.PaintEventHandler( this.panelメッセージ本文_Paint );
			// 
			// CPopupメッセージ
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ControlBox = false;
			this.Controls.Add( this.panelメッセージ本文 );
			this.Controls.Add( this.pictureBoxイラスト );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "CPopupメッセージ";
			this.Load += new System.EventHandler( this.Popupメッセージ_Load );
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.Popupメッセージ_FormClosing );
			( (System.ComponentModel.ISupportInitialize) ( this.pictureBoxイラスト ) ).EndInit();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBoxイラスト;
		private System.Windows.Forms.Panel panelメッセージ本文;
	}
}