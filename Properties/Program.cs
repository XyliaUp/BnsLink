using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
namespace BnsLink
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main(string[] args = null)
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);


			//重读dll后的执行方法请置于此线程下
			Thread thread = new Thread((ThreadStart)delegate
			{
				Application.Run(new Form1());
			});

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		/// <summary>
		/// 加载静态dll资源用，在调试模式下请注释调用
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string tmpName = new AssemblyName(args.Name).Name;
			string resourceName = "BnsLink..dll." + tmpName + ".dll";

			try
			{
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
				{
					if (stream is null) return null;


					byte[] assemblyData = new byte[stream.Length];

					stream.Read(assemblyData, 0, assemblyData.Length);

					return Assembly.Load(assemblyData);
				}
			}
			catch (Exception ee)
			{
				if (!string.IsNullOrWhiteSpace(tmpName))
				{
					//转为小写形式
					tmpName = tmpName.ToLower();

					if (!tmpName.Contains(".resources") && !tmpName.Contains(".xmlseri") && !tmpName.Contains(".font"))
					{
						MessageBox.Show("在读取" + tmpName + "时\n发生了以下问题：" + ee.Message, "资源读取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}

				return null;
			}
		}
	}
}
