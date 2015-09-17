using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SRTS
{
    public class ServiceTypes
    {
        static Dictionary<string, TypeScriptType> hubCache;
        static Dictionary<string, string> hubAliasesCache;
        static Dictionary<string, TypeScriptType> clientCache;

        public static Dictionary<string, TypeScriptType> HubCache { get { return hubCache; } }
        public static Dictionary<string, TypeScriptType> ClientCache { get { return clientCache; } }
        public static Dictionary<string, string> HubAliasesCache { get { return hubAliasesCache; } }

        static ServiceTypes() 
        {
            hubCache = new Dictionary<string, TypeScriptType>();
            clientCache = new Dictionary<string, TypeScriptType>();
            hubAliasesCache = new Dictionary<string, string>();
        }

        public static string CamelCase(string s)
        {
            return s[0].ToString().ToLower() + s.Substring(1);
        }

        static TypeScriptType MakeHubInterface(Type hubType) 
        {
            var name = "I" + hubType.Name;
            var cName = name + "Client";

            var declaration = "interface " + name + " {\n";
            var hubAttribute = "";
            hubType.GetMethods()
                .Where(mi => mi.GetBaseDefinition().DeclaringType.Name == hubType.Name).ToList()
                .ForEach(mi =>
                {                    
                    declaration += GetMethodDeclaration(mi);
                });
            
            declaration += "}";

            hubAttribute = hubType.CustomAttributes
                .Where(ad => ad.AttributeType.Name == "HubNameAttribute")
                .Select(ad => ad.ConstructorArguments.FirstOrDefault().Value.ToString())
                .FirstOrDefault();

            var ret = new TypeScriptType
            {
                Name = name,
                Declaration = declaration
            };

            hubCache.Add(name, ret);

            hubAliasesCache.Add(name, hubAttribute);

            clientCache.Add(cName, new TypeScriptType { 
                Name = cName,
                Declaration = GetClientDeclaration(hubType, cName)
            });

            return ret;
        }

        private static string GetClientDeclaration(Type hubType, string cName)
        {
            if (hubType.BaseType.Name == "Hub")
            {
                return "interface " + cName + " {  \n    /* Not implemented */ \n}";
            }
            var clientInterface = hubType.BaseType.GenericTypeArguments[0];
            var result = "interface " + cName + " {  \n    /* implements " + clientInterface.Name + " */\n";
            clientInterface.GetMethods()
                .Where(mi => mi.DeclaringType.Name == clientInterface.Name).ToList()
                .ForEach(mi =>
                {
                    result += GetMethodDeclaration(mi);
                });
            result += "}";
            return result;
        }

        private static string GetMethodDeclaration(MethodInfo mi)
        {
            var count = 0;
            var result = "    " + CamelCase(mi.Name) + "(";

            var retTS = DataTypes.GetTypeScriptType(mi.ReturnType);
            var retType = retTS.Name == "void" ? "void" : "IPromise<" + retTS.Name + ">";
            mi.GetParameters().ToList()
                .ForEach((pi) =>
                {
                    var sep = (count != 0 ? ", " : "");
                    count++;
                    var tst = DataTypes.GetTypeScriptType(pi.ParameterType);
                    result += sep + pi.Name + ": " + tst.Name;
                });

            result += "): " + retType + ";\n";
            return result;
        }

        /*
         declare var client: IClient // To be filled by the user...
         declare var server: IServer
         
         */

        public static void AddHubsFromAssembly(Assembly assembly) 
        {            
            assembly.GetTypes()
	            .Where(t => t.BaseType != null && t.BaseType.Name == "Hub").ToList()
                .ForEach(t => MakeHubInterface(t) );

            assembly.GetTypes()
                .Where(t => t.BaseType != null && t.BaseType.Name == "Hub`1").ToList()
                .ForEach(t => MakeHubInterface(t));
        }
    }
}
