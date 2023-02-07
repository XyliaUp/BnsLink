using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BnsLink
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			CheckForIllegalCrossThreadCalls = false;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					if (Directory.Exists(textBox2.Text))
					{
						if (MessageBox.Show("判断到文件夹已经存在，是否确认继续？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
						{
							try
							{
								//Directory.Delete(textBox2.Text, true);
							}
							catch
							{

							}
						}
						else return;
					}



					Util.Compress exec = new Util.Compress();

					exec.outputPath = textBox2.Text;
					exec.archiveFileName = Application.StartupPath + "/data.7z";
					//exec.password = "Xylia&20200917Aoptd=";

					exec.ExtractFiles(true, act => progressBar1.Value = act);


					//创建软连接
					CreateLink();

					progressBar1.Value = 100;
				}
				catch (Exception ee)
				{
					MessageBox.Show(ee.Message, "异常提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Debug.WriteLine(ee);
				}
			});

			thread.Start();
		}

		/// <summary>
		/// 创建软连接
		/// </summary>
		public void CreateLink()
		{
			List<string> Commands = new List<string>();
			foreach(var line in Properties.Resources.link.Split('\n'))
			{
				if (!line.StartsWith("//") && !string.IsNullOrWhiteSpace(line))
				{
					Commands.Add(line
						.Replace("{Ori}", textBox2.Text).Replace("{ori}", textBox2.Text)
						.Replace("{Tar}", textBox1.Text).Replace("{tar}", textBox1.Text));
				}
			}

			Commands.ForEach(c => new Cmd(c));

			new DirectoryInfo(textBox2.Text + @"\contents\Local\Garena\data").Attributes = FileAttributes.Hidden;
			new DirectoryInfo(textBox2.Text + @"\contents\Local\Garena\THAI\data").Attributes = FileAttributes.Hidden;
		}


		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void button2_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog Folder = new FolderBrowserDialog();

			if (Folder.ShowDialog() == DialogResult.OK)
			{
				textBox1.Text = Folder.SelectedPath;
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog Folder = new FolderBrowserDialog();

			if (Folder.ShowDialog() == DialogResult.OK)
			{
				textBox2.Text = Folder.SelectedPath;
			}
		}
	}
}
