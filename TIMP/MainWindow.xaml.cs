using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TIMP
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        const string TemplateFile = "template.tbl";
        static readonly PlatformID[] WindowsPlatformIDs = new PlatformID[]{
                PlatformID.Win32S,
                PlatformID.Win32Windows,
                PlatformID.Win32NT,
                PlatformID.WinCE};
        static List<string> Template { get; set; }

        Dictionary<string, List<UIElement>> formElements;
        List<string> files = new List<string>();
        static bool isProtectionActive = false;
        static bool alreadyInMenu = false;

        public MainWindow()
        {
            InitializeComponent();
            formElements = SetGroups();
            if (File.Exists(TemplateFile))
            {
                Free(TemplateFile);
                Template = File.ReadAllLines(TemplateFile).ToList();
                Protect(TemplateFile);
                Activate();
            }
        }

        static void Protect(string path)
        {
            var t = File.GetAccessControl(path);
            foreach (FileSystemAccessRule r in t.GetAccessRules(true, true, typeof(NTAccount)))
            {
                t.AddAccessRule(
                    new FileSystemAccessRule(r.IdentityReference,
                                             FileSystemRights.FullControl,
                                             AccessControlType.Deny));
            }
            File.SetAccessControl(path, t);
        }

        static void Free(string path)
        {
            var t = File.GetAccessControl(path);
            foreach (FileSystemAccessRule r in t.GetAccessRules(true, true, typeof(NTAccount)))
            {
                t.RemoveAccessRule(r);
            }
            File.SetAccessControl(path, t);
        }

        static void Deactivate()
        {
            for (int i = 1; i < Template.Count; i++)
            {
                if (!File.Exists(Template[i])) continue;
                Free(Template[i]);
            }
            Free(TemplateFile);
            using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
            {
                file.WriteLine(Template[0]);
            }
            Protect(TemplateFile);
            isProtectionActive = false;
        }

        static void Activate()
        {
            for (int i = 1; i < Template.Count; i++)
            {
                if (!File.Exists(Template[i]))
                {
                    File.Create(Template[i]).Dispose();
                }
                Protect(Template[i]);
            }
            Free(TemplateFile);
            using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
            {
                file.WriteLine(Template[0]);
            }
            Protect(TemplateFile);
            isProtectionActive = true;
        }

        static void ChangeTable(bool delete = false)
        {
            if (delete)
            {
                File.Delete(TemplateFile);
                Process.Start(Assembly.GetExecutingAssembly().Location);
                Environment.Exit(0);
            }
            var fileCopy = "." + TemplateFile;
            using (var t = File.CreateText(fileCopy))
            {
                for (int i = 1; i < Template.Count; i++)
                {
                    t.WriteLine(Template[i]);
                }
                t.Flush();
            }
            Process p;
            if (WindowsPlatformIDs.Contains(System.Environment.OSVersion.Platform)) p = Process.Start("notepad.exe", fileCopy);
            else p = Process.Start("nano", fileCopy); 
            p.WaitForExit();
            if (File.Exists(fileCopy))
            {
                var t = File.ReadLines(fileCopy);
                Template = Template.GetRange(0, 1).Concat(t).ToList();
                Free(TemplateFile);
                File.WriteAllLines(TemplateFile, Template);
                Protect(TemplateFile);
                File.Delete(fileCopy);
            }
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TemplateFile))
            {
                MessageBoxResult crFile = MessageBox.Show($"File {TemplateFile} is not found in current directory...\nDo you want to create?", "Create a file", MessageBoxButton.YesNo);
                if (crFile == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Create(TemplateFile).Dispose();
                        MessageBox.Show($"File {TemplateFile} was created");
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message);
                    }
                }
                else
                {
                    MessageBoxResult areYouSure = MessageBox.Show($"Without a {TemplateFile} file this program won't run.\n" +
                        $"If you press \"No\", application will close.\n\nCreate the {TemplateFile} file?", "Are you sure?...", MessageBoxButton.YesNo);
                    if (areYouSure == MessageBoxResult.Yes)
                    {
                        try
                        {
                            File.Create(TemplateFile).Dispose();
                            MessageBox.Show($"File {TemplateFile} was created");
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Goodbye then...", "Okay...", MessageBoxButton.OK);
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }
            else
            {
                Free(TemplateFile);
                Template = File.ReadAllLines(TemplateFile).ToList();
                Protect(TemplateFile);

                OpenStartForm(false);
                OpenCheckPasswordForm(true);
                return;
            }


            OpenStartForm(false);
            OpenSetPasswordForm(true);
        }

        private void enterPassClick(object sender, RoutedEventArgs e)
        {
            if (checkPasswordTextBox.Text != "")
            {
                var password = checkPasswordTextBox.Text;
                using (var hashf = SHA256.Create())
                {
                    var hashp = hashf.ComputeHash(Encoding.Default.GetBytes(password));
                    if (Template[0] == Encoding.Default.GetString(hashp))
                    {
                        MessageBox.Show("Correct");
                        checkPasswordTextBox.Text = "";
                    }
                    else
                    {
                        MessageBox.Show("Password is incorrect");
                        checkPasswordTextBox.Text = "";
                        return;
                    }
                    OpenCheckPasswordForm(false);
                    if (alreadyInMenu)
                    {
                        OpenSetPasswordForm(true);
                    }
                    else
                    {
                        OpenMainMenuForm(true);
                        alreadyInMenu = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, enter at least 1 symbol...", "Something went wrong...", MessageBoxButton.OK);
            }
        }


        private void setPasswordClick(object sender, RoutedEventArgs e)
        {
            if (setPasswordTextBox.Text == setPasswordTextBox2.Text)
            {
                Free(TemplateFile);
                var password = setPasswordTextBox.Text;
                if (Template is null)
                {
                    using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
                    {
                        using (var hashf = SHA256.Create())
                            file.WriteLine(Encoding.Default.GetString(hashf.ComputeHash(Encoding.Default.GetBytes(password))));
                    }
                }
                else
                {
                    using (var hashf = SHA256.Create())
                    {
                        Template[0] = Encoding.Default.GetString(hashf.ComputeHash(Encoding.Default.GetBytes(password)));
                        using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
                        {
                            file.Write(Template[0]);
                        }
                    }
                }
                Template = File.ReadAllLines(TemplateFile).ToList();
                Protect(TemplateFile);

                setPasswordTextBox.Text = setPasswordTextBox2.Text = "";

                OpenSetPasswordForm(false);
                if (alreadyInMenu)
                {
                    OpenMainMenuForm(true);
                }
                else
                {
                    OpenEnterFilesForm(true);
                }
            }
            else
            {
                MessageBox.Show("Passwords do not match. Please, retry.", "Something went wrong...", MessageBoxButton.OK);
                setPasswordTextBox.Text = "";
                setPasswordTextBox2.Text = "";
            }
        }


        private void addFileClick(object sender, RoutedEventArgs e)
        {
            files.Add(enterFilesTextBox.Text);
            enterFilesLabel2.Content += $"\n{enterFilesTextBox.Text}";
            enterFilesTextBox.Text = "";
        }

        private void confirmFilesClick(object sender, RoutedEventArgs e)
        {
            Free(TemplateFile);
            Template = Template.GetRange(0, 1).Concat(files).ToList();
            File.WriteAllLines(TemplateFile, Template);
            Protect(TemplateFile);
            Activate();
            OpenEnterFilesForm(false);
            OpenCheckPasswordForm(true);
        }

        private void protectionButtonClick(object sender, RoutedEventArgs e)
        {
            if (isProtectionActive)
            {
                Deactivate();
                mainMenuProtectionButton.Content = "ACTIVATE PROTECTION";
                mainMenuProtectionStatusLabel.Content = "Protection status: Inactive";
                isProtectionActive = false;
            }
            else
            {
                Activate();
                mainMenuProtectionButton.Content = "DEACTIVATE PROTECTION";
                mainMenuProtectionStatusLabel.Content = "Protection status: Active";
                isProtectionActive = true;
            }
        }

        private void mainMenuChangeTableButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeTable();
        }

        private void mainMenuDeleteTableButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeTable(true);
        }

        private void mainMenuChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMainMenuForm(false);
            OpenCheckPasswordForm(true);
        }

        private void mainMenuExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        Dictionary<string, List<UIElement>> SetGroups()
        {
            Dictionary<string, List<UIElement>> elements = new Dictionary<string, List<UIElement>>();

            List<UIElement> startForm = new List<UIElement>();
            startForm.Add(startLabel);
            startForm.Add(startButton);
            elements.Add("startForm", startForm);

            List<UIElement> setPasswordForm = new List<UIElement>();
            setPasswordForm.Add(setPasswordButton);
            setPasswordForm.Add(setPasswordLabel);
            setPasswordForm.Add(setPasswordLabel2);
            setPasswordForm.Add(setPasswordLabel3);
            setPasswordForm.Add(setPasswordTextBox);
            setPasswordForm.Add(setPasswordTextBox2);
            setPasswordForm.Add(setPasswordBorder);
            setPasswordForm.Add(setPasswordBorder2);
            elements.Add("setPasswordForm", setPasswordForm);

            List<UIElement> enterFilesForm = new List<UIElement>();
            enterFilesForm.Add(enterFilesLabel);
            enterFilesForm.Add(enterFilesLabel2);
            enterFilesForm.Add(enterFilesButton);
            enterFilesForm.Add(enterFilesButton2);
            enterFilesForm.Add(enterFilesTextBox);
            enterFilesForm.Add(enterFilesBorder);
            elements.Add("enterFilesForm", enterFilesForm);

            List<UIElement> checkPasswordForm = new List<UIElement>();
            checkPasswordForm.Add(checkPasswordTextBox);
            checkPasswordForm.Add(checkPasswordMessage);
            checkPasswordForm.Add(checkPasswordEnterPasswordButton);
            checkPasswordForm.Add(checkPasswordBorder);
            elements.Add("checkPasswordForm", checkPasswordForm);

            List<UIElement> mainMenuForm = new List<UIElement>();
            mainMenuForm.Add(mainMenuExitButton);
            mainMenuForm.Add(mainMenuChangePasswordButton);
            mainMenuForm.Add(mainMenuChangeTableButton);
            mainMenuForm.Add(mainMenuDeleteTableButton);
            mainMenuForm.Add(mainMenuProtectionButton);
            mainMenuForm.Add(mainMenuProtectionStatusLabel);
            mainMenuForm.Add(mainMenuLabel);
            elements.Add("mainMenuForm", mainMenuForm);


            return elements;
        }

        void OpenMainMenuForm(bool show)
        {
            if (show)
            {
                foreach (var item in formElements["mainMenuForm"])
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            else
            {
                foreach (var item in formElements["mainMenuForm"])
                {
                    item.Visibility = Visibility.Hidden;
                }
            }
        }
        void OpenEnterFilesForm(bool show)
        {
            if (show)
            {
                foreach (var item in formElements["enterFilesForm"])
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            else
            {
                foreach (var item in formElements["enterFilesForm"])
                {
                    item.Visibility = Visibility.Hidden;
                }
            }
        }
        void OpenSetPasswordForm(bool show)
        {
            if (show)
            {
                foreach (var item in formElements["setPasswordForm"])
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            else
            {
                foreach (var item in formElements["setPasswordForm"])
                {
                    item.Visibility = Visibility.Hidden;
                }
            }
        }
        void OpenCheckPasswordForm(bool show)
        {
            if (show)
            {
                foreach(var item in formElements["checkPasswordForm"])
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            else
            {
                foreach (var item in formElements["checkPasswordForm"])
                {
                    item.Visibility = Visibility.Hidden;
                }
            }
        }

        void OpenStartForm(bool show)
        {
            if (show)
            {
                foreach (var item in formElements["startForm"])
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            else
            {
                foreach (var item in formElements["startForm"])
                {
                    item.Visibility = Visibility.Hidden;
                }
            }
        }     
    }
}
