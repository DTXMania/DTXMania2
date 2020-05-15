namespace SSTFEditor
{
	partial class オプションダイアログ
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(オプションダイアログ));
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.tabControlオプション = new System.Windows.Forms.TabControl();
            this.tabPage全般 = new System.Windows.Forms.TabPage();
            this.checkBoxSSTF変換通知ダイアログ = new System.Windows.Forms.CheckBox();
            this.buttonViewerPath参照 = new System.Windows.Forms.Button();
            this.textBoxViewerPath = new System.Windows.Forms.TextBox();
            this.labelViewerPath = new System.Windows.Forms.Label();
            this.checkBoxオートフォーカス = new System.Windows.Forms.CheckBox();
            this.label個まで表示する = new System.Windows.Forms.Label();
            this.checkBox最近使用したファイル = new System.Windows.Forms.CheckBox();
            this.numericUpDown最近使用したファイルの最大表示個数 = new System.Windows.Forms.NumericUpDown();
            this.tabControlオプション.SuspendLayout();
            this.tabPage全般.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown最近使用したファイルの最大表示個数)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // tabControlオプション
            // 
            resources.ApplyResources(this.tabControlオプション, "tabControlオプション");
            this.tabControlオプション.Controls.Add(this.tabPage全般);
            this.tabControlオプション.Name = "tabControlオプション";
            this.tabControlオプション.SelectedIndex = 0;
            // 
            // tabPage全般
            // 
            this.tabPage全般.Controls.Add(this.checkBoxSSTF変換通知ダイアログ);
            this.tabPage全般.Controls.Add(this.buttonViewerPath参照);
            this.tabPage全般.Controls.Add(this.textBoxViewerPath);
            this.tabPage全般.Controls.Add(this.labelViewerPath);
            this.tabPage全般.Controls.Add(this.checkBoxオートフォーカス);
            this.tabPage全般.Controls.Add(this.label個まで表示する);
            this.tabPage全般.Controls.Add(this.checkBox最近使用したファイル);
            this.tabPage全般.Controls.Add(this.numericUpDown最近使用したファイルの最大表示個数);
            resources.ApplyResources(this.tabPage全般, "tabPage全般");
            this.tabPage全般.Name = "tabPage全般";
            this.tabPage全般.UseVisualStyleBackColor = true;
            // 
            // checkBoxSSTF変換通知ダイアログ
            // 
            resources.ApplyResources(this.checkBoxSSTF変換通知ダイアログ, "checkBoxSSTF変換通知ダイアログ");
            this.checkBoxSSTF変換通知ダイアログ.Name = "checkBoxSSTF変換通知ダイアログ";
            this.checkBoxSSTF変換通知ダイアログ.UseVisualStyleBackColor = true;
            // 
            // buttonViewerPath参照
            // 
            resources.ApplyResources(this.buttonViewerPath参照, "buttonViewerPath参照");
            this.buttonViewerPath参照.Name = "buttonViewerPath参照";
            this.buttonViewerPath参照.UseVisualStyleBackColor = true;
            this.buttonViewerPath参照.Click += new System.EventHandler(this.buttonViewerPath参照_Click);
            // 
            // textBoxViewerPath
            // 
            resources.ApplyResources(this.textBoxViewerPath, "textBoxViewerPath");
            this.textBoxViewerPath.Name = "textBoxViewerPath";
            // 
            // labelViewerPath
            // 
            resources.ApplyResources(this.labelViewerPath, "labelViewerPath");
            this.labelViewerPath.Name = "labelViewerPath";
            // 
            // checkBoxオートフォーカス
            // 
            resources.ApplyResources(this.checkBoxオートフォーカス, "checkBoxオートフォーカス");
            this.checkBoxオートフォーカス.Name = "checkBoxオートフォーカス";
            this.checkBoxオートフォーカス.UseVisualStyleBackColor = true;
            // 
            // label個まで表示する
            // 
            resources.ApplyResources(this.label個まで表示する, "label個まで表示する");
            this.label個まで表示する.Name = "label個まで表示する";
            // 
            // checkBox最近使用したファイル
            // 
            resources.ApplyResources(this.checkBox最近使用したファイル, "checkBox最近使用したファイル");
            this.checkBox最近使用したファイル.Name = "checkBox最近使用したファイル";
            this.checkBox最近使用したファイル.UseVisualStyleBackColor = true;
            // 
            // numericUpDown最近使用したファイルの最大表示個数
            // 
            resources.ApplyResources(this.numericUpDown最近使用したファイルの最大表示個数, "numericUpDown最近使用したファイルの最大表示個数");
            this.numericUpDown最近使用したファイルの最大表示個数.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown最近使用したファイルの最大表示個数.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown最近使用したファイルの最大表示個数.Name = "numericUpDown最近使用したファイルの最大表示個数";
            this.numericUpDown最近使用したファイルの最大表示個数.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // オプションダイアログ
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ControlBox = false;
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.tabControlオプション);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "オプションダイアログ";
            this.tabControlオプション.ResumeLayout(false);
            this.tabPage全般.ResumeLayout(false);
            this.tabPage全般.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown最近使用したファイルの最大表示個数)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TabControl tabControlオプション;
		private System.Windows.Forms.TabPage tabPage全般;
		internal System.Windows.Forms.CheckBox checkBoxオートフォーカス;
		private System.Windows.Forms.Label label個まで表示する;
		internal System.Windows.Forms.CheckBox checkBox最近使用したファイル;
		internal System.Windows.Forms.NumericUpDown numericUpDown最近使用したファイルの最大表示個数;
		private System.Windows.Forms.Button buttonViewerPath参照;
		private System.Windows.Forms.Label labelViewerPath;
		internal System.Windows.Forms.TextBox textBoxViewerPath;
        internal System.Windows.Forms.CheckBox checkBoxSSTF変換通知ダイアログ;
    }
}