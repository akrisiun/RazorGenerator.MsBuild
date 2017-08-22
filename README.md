# RazorGenerator.MsBuild

```
Error	19
RazorCodeGen
NullReferenceException: Object reference not set to an instance of an object.
at System.Configuration.TypeUtil.GetTypeWithReflectionPermission(
IInternalConfigHost host, String typeString, Boolean throwOnError)
at System.Configuration.MgmtConfigurationRecord.CreateSectionFactory(FactoryRecord factoryRecord)
at System.Configuration.BaseConfigurationRecord.FindAndEnsureFactoryRecord(
String configKey, Boolean& isRootDeclaredHere)
c:\bin\RazorGenerator.MsBuild\RazorGenerator.MsBuild.targets	28	9	DocumentE

override protected object CreateSectionFactory(FactoryRecord factoryRecord) {

// Get the type of the factory
Type type = TypeUtil.GetTypeWithReflectionPermission(Host, factoryRecord.FactoryTypeName, true);

//
// If the type is not a ConfigurationSection, use the DefaultSection if the type
// implements IConfigurationSectionHandler.
//
if (!typeof(ConfigurationSection).IsAssignableFrom(type)) {
TypeUtil.VerifyAssignableType(typeof(IConfigurationSectionHandler), type, true);
type = typeof(DefaultSection);
}

ConstructorInfo ctor = TypeUtil.GetConstructorWithReflectionPermission(type, typeof(ConfigurationSection), true);

return ctor;
}
```

RazorGenerator.MsBuild minimal build clone.   
For .cshtml files to compile on develpment machine, not on release web server.

```
  <Import Project="\RazorGenerator.MsBuild\RazorGenerator.MsBuild.targets" />
  <ItemGroup>
    <RazorSrcFiles Include="Views\*.cshtml" />
    <RazorSrcFiles Include="Views\Home\*.cshtml" />

```

Orinal source:
https://github.com/RazorGenerator/RazorGenerator
