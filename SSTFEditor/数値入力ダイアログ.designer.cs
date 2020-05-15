namespace SSTFEditor
{
	partial class 数値入力ダイアログ
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( 数値入力ダイアログ ) );
			this.labelメッセージ = new System.Windows.Forms.Label();
			this.numericUpDown数値 = new System.Windows.Forms.NumericUpDown();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonキャンセル = new System.Windows.Forms.Button();
			( (System.ComponentModel.ISupportInitialize) ( this.numericUpDown数値 ) ).BeginInit();
			this.SuspendLayout();
			// 
			// labelメッセージ
			// 
			resources.ApplyResources( this.labelメッセージ, "labelメッセージ" );
			this.labelメッセージ.Name = "labelメッセージ";
			// 
			// numericUpDown数値
			// 
			this.numericUpDown数値.DecimalPlaces = 4;
			resources.ApplyResources( this.numericUpDown数値, "numericUpDown数値" );
			this.numericUpDown数値.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
			this.numericUpDown数値.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            262144} );
			this.numericUpDown数値.Name = "numericUpDown数値";
			this.numericUpDown数値.Value = new decimal( new int[] {
            1,
            0,
            0,
            262144} );
			this.numericUpDown数値.KeyDown += new System.Windows.Forms.KeyEventHandler( this.numericUpDown数値_KeyDown );
			// 
			// buttonOK
			// 
			resources.ApplyResources( this.buttonOK, "buttonOK" );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonキャンセル
			// 
			resources.ApplyResources( this.buttonキャンセル, "buttonキャンセル" );
			this.buttonキャンセル.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonキャンセル.Name = "buttonキャンセル";
			this.buttonキャンセル.UseVisualStyleBackColor = true;
			// 
			// C数値入力ダイアログ
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.buttonキャンセル );
			this.Controls.Add( this.numericUpDown数値 );
			this.Controls.Add( this.labelメッセージ );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "C数値入力ダイアログ";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			( (System.ComponentModel.ISupportInitialize) ( this.numericUpDown数値 ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelメッセージ;
		private System.Windows.Forms.NumericUpDown numericUpDown数値;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonキャンセル;
	}
}