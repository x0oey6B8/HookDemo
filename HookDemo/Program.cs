using System.Windows.Forms;

namespace HookDemo
{
    class Program
    {
        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new Form1().Show();
            // フックを行うにはメッセージループ（ウィンドウ）が必要
            Application.Run();
        }
    }
}
