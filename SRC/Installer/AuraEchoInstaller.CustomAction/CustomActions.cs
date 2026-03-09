using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using WixToolset.Dtf.WindowsInstaller;

namespace AuraEchoInstaller.CustomAction
{
    public class CustomActions
    {
        private const string TaskName = "AuraEchoAutoStart";
        [CustomAction]
        public static ActionResult CreateAuraEchoAutoStartTask(Session session)
        {
            session.Log("开始创建计划任务...");

            try
            {
                // 获取安装路径，注意：在 deferred 模式下需通过 CustomActionData 获取
                string installFolder = session.CustomActionData["INSTALLFOLDER"];
                string exePath = System.IO.Path.Combine(installFolder, "AuraEchoLauncher.exe");

                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "AuraEcho 的开机自动启动功能依赖此任务。如果此任务被禁用或删除，AuraEcho 将无法在开机后自动启动。";

                    SecurityIdentifier adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    td.Principal.GroupId = adminSid.Value;
                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    td.Triggers.Add(new LogonTrigger());

                    td.Actions.Add(new ExecAction(exePath, "-hide", installFolder));

                    td.Settings.DisallowStartIfOnBatteries = false;
                    td.Settings.StopIfGoingOnBatteries = false;
                    td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                    ts.RootFolder.RegisterTaskDefinition(
                        TaskName,
                        td,
                        TaskCreation.CreateOrUpdate,
                        null,
                        null,
                        TaskLogonType.Group);
                }

                session.Log("计划任务创建成功。");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("创建计划任务失败: " + ex.ToString());
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult DeleteAuraEchoAutoStartTask(Session session)
        {
            try
            {
                using (var ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(TaskName, false);
                }
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("删除任务失败: " + ex.Message);
                return ActionResult.Success; // 卸载时通常建议忽略错误，防止卸载中断
            }
        }

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