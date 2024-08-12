using Microsoft.Win32;

namespace NtoLib.Recipes.MbeTable
{
    internal class Reg
    {
        private static string _OOOLSI = "LabSI";
        private static string _PrgName = "HDV";

        private static void _set_value(RegistryKey key, string addr, string value)
        {
            if (string.IsNullOrEmpty(addr))
                return;
            int num1 = 0;
            while (num1 < addr.Length && addr[num1] == '/')
                ++num1;
            if (num1 >= addr.Length)
                return;
            string name = addr.Substring(num1);
            int num2 = name.IndexOf('/');
            if (num2 < 0)
            {
                key.SetValue(name, (object)value);
            }
            else
            {
                RegistryKey subKey = key.CreateSubKey(name.Substring(0, num2), RegistryKeyPermissionCheck.ReadWriteSubTree);
                Reg._set_value(subKey, name.Substring(num2), value);
                subKey.Close();
            }
        }

        public static void set_value(string addr, string value)
        {
            RegistryKey currentUser = Registry.CurrentUser;
            RegistryKey registryKey = currentUser.OpenSubKey("Software", true);
            RegistryKey subKey1 = registryKey.CreateSubKey(Reg._OOOLSI, RegistryKeyPermissionCheck.ReadWriteSubTree);
            RegistryKey subKey2 = subKey1.CreateSubKey(Reg._PrgName, RegistryKeyPermissionCheck.ReadWriteSubTree);
            Reg._set_value(subKey2, addr, value);
            subKey2.Close();
            subKey1.Close();
            registryKey.Close();
            currentUser.Close();
        }

        private static string _get_value(RegistryKey key, string addr)
        {
            if (string.IsNullOrEmpty(addr))
                return "";
            int num1 = 0;
            while (num1 < addr.Length && addr[num1] == '/')
                ++num1;
            if (num1 >= addr.Length)
                return "";
            string name = addr.Substring(num1);
            int num2 = name.IndexOf('/');
            if (num2 < 0)
            {
                object obj = key.GetValue(name);
                return obj == null ? "" : obj.ToString();
            }
            RegistryKey subKey = key.CreateSubKey(name.Substring(0, num2), RegistryKeyPermissionCheck.ReadWriteSubTree);
            string str = Reg._get_value(subKey, name.Substring(num2));
            subKey.Close();
            return str;
        }

        public static string get_value(string addr)
        {
            RegistryKey currentUser = Registry.CurrentUser;
            RegistryKey registryKey = currentUser.OpenSubKey("Software", true);
            RegistryKey subKey1 = registryKey.CreateSubKey(Reg._OOOLSI, RegistryKeyPermissionCheck.ReadWriteSubTree);
            RegistryKey subKey2 = subKey1.CreateSubKey(Reg._PrgName, RegistryKeyPermissionCheck.ReadWriteSubTree);
            string str = Reg._get_value(subKey2, addr);
            subKey2.Close();
            subKey1.Close();
            registryKey.Close();
            currentUser.Close();
            return str;
        }
    }
}
