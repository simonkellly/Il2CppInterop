using System;
using System.Linq;
using Il2CppInterop.Generator.Contexts;
using Il2CppInterop.Generator.Extensions;
using Mono.Cecil;

namespace Il2CppInterop.Generator.Passes
{
    public static class Pass21GenerateValueTypeFields
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                var il2CppTypeTypeRewriteContext = assemblyContext.GlobalContext.GetAssemblyByName("mscorlib").GetTypeByName("System.Object");
                var il2CppSystemTypeRef = assemblyContext.NewAssembly.MainModule.ImportReference(il2CppTypeTypeRewriteContext.NewType);

                foreach (var typeContext in assemblyContext.Types)
                {
                    if (typeContext.ComputedTypeSpecifics != TypeRewriteContext.TypeSpecifics.BlittableStruct || typeContext.OriginalType.IsEnum) continue;

                    var newType = typeContext.NewType;
                    newType.Attributes = newType.Attributes & ~(TypeAttributes.LayoutMask) |
                                         TypeAttributes.ExplicitLayout;

                    ILGeneratorEx.GenerateBoxMethod(newType, typeContext.ClassPointerFieldRef, il2CppSystemTypeRef);

                    foreach (var fieldContext in typeContext.Fields)
                    {
                        var field = fieldContext.OriginalField;
                        if (field.IsStatic) continue;

                        var newField = new FieldDefinition(fieldContext.UnmangledName, field.Attributes.ForcePublic(),
                            !field.FieldType.IsValueType
                                ? assemblyContext.Imports.IntPtr
                                : assemblyContext.RewriteTypeRef(field.FieldType));

                        newField.Offset = Convert.ToInt32(
                            (string)field.CustomAttributes
                                .Single(it => it.AttributeType.Name == "FieldOffsetAttribute")
                                .Fields.Single().Argument.Value, 16);

                        newType.Fields.Add(newField);
                    }
                }
            }
        }
    }
}