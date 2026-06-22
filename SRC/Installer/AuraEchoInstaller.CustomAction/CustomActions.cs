using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;
using WixToolset.Dtf.WindowsInstaller;

namespace AuraEchoInstaller.CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult MigrationDataBase(Session session)
        {
            session.Log("开始迁移数据库...");
            using (Record record = new Record(2))
            {
                record[1] = "MigrationDataBase";
                record[2] = "正在配置数据库...";
                session.Message(InstallMessage.ActionStart, record);
            }
            try
            {
                string dataMigratorPath = session["CustomActionData"];
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = dataMigratorPath,
                    WorkingDirectory = Path.GetDirectoryName(dataMigratorPath),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        session.Log("数据库迁移失败，退出代码: " + process.ExitCode);
                        return ActionResult.Failure;
                    }
                }
                session.Log("数据库迁移成功。");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("数据库迁移失败: " + ex.ToString());
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult RemoveRunAtBootRegistry(Session session)
        {
            session.Log("RemoveRunAtBootRegistry Begin");
            using (Record record = new Record(2))
            {
                record[1] = "CleanRunAtBootRegistry";
                record[2] = "正在清理启动设置项...";
                session.Message(InstallMessage.ActionStart, record);
            }
            try
            {
                const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
                const string STARTUP_APPROVED_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
                using (RegistryKey startupApprovedKey = Registry.LocalMachine.OpenSubKey(STARTUP_APPROVED_KEY_PATH, true))
                {
                    if (startupApprovedKey.GetValue("AuraEcho") != null)
                    {
                        startupApprovedKey.DeleteValue("AuraEcho");
                        session.Log("已删除注册表中的启动批准项。");
                    }
                }

                using (RegistryKey itemKeyRoot = Registry.LocalMachine.OpenSubKey(RUN_KEY_PATH, true))
                {
                    if (itemKeyRoot.GetValue("AuraEcho") != null)
                    {
                        itemKeyRoot.DeleteValue("AuraEcho");
                        session.Log("已删除注册表中的启动项。");
                    }
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("删除注册表开机启动项失败: " + ex.ToString());
                return ActionResult.Failure;
            }
            finally
            {
                session.Log("RemoveRunAtBootRegistry Begin");
            }
        }

        [CustomAction]
        public static ActionResult CleanupLocalData(Session session)
        {
            string shouldRemove = session.CustomActionData["REMOVE_DATA"];
            if (shouldRemove != "1") return ActionResult.Success;

            string path = @"C:\ProgramData\AuraEcho";

            try
            {
                if (!Directory.Exists(path))
                {
                    session.Log($"路径不存在：{path}");
                    return ActionResult.Failure;
                }

                session.Log("正在清理本地数据...");
                using (Record record = new Record(2))
                {
                    record[1] = "CleanRunAtBootRegistry";
                    record[2] = "正在清理本地数据...";
                    session.Message(InstallMessage.ActionStart, record);
                }
                Directory.Delete(path, true);
            }
            catch (System.Exception ex)
            {
                session.Log("清理失败: " + ex.Message);
                return ActionResult.Failure;
            }
            return ActionResult.Success;
        }
    }
}