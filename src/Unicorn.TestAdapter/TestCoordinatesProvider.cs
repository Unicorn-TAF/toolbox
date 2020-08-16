using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Unicorn.TestAdapter
{
    public class TestCoordinatesProvider
    {
        readonly string _assemblyPath;
        IDictionary<string, TypeDefinition> _typeDefs;

        public TestCoordinatesProvider(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public TestCoordinates GetNavigationData(string className, string methodName)
        {
            if (_typeDefs == null)
            {
                _typeDefs = CacheTypes(_assemblyPath);
            }

            TypeDefinition typeDef;

            if (!_typeDefs.TryGetValue(className, out typeDef))
            {
                return TestCoordinates.Invalid;
            }
                

            MethodDefinition methodDef = null;

            while (true)
            {
                methodDef = typeDef
                    .GetMethods()
                    .FirstOrDefault(o => o.Name == methodName);

                if (methodDef != null)
                {
                    break;
                }

                var baseType = typeDef.BaseType;

                if (baseType == null || baseType.FullName == "System.Object")
                {
                    return TestCoordinates.Invalid;
                }
                    
                typeDef = typeDef.BaseType.Resolve();
            }

            var sequencePoint = FirstOrDefaultSequencePoint(methodDef);

            return sequencePoint == null ? 
                TestCoordinates.Invalid : 
                new TestCoordinates(sequencePoint.Document.Url, sequencePoint.StartLine);
        }

        static bool DoesPdbFileExist(string filepath) => File.Exists(Path.ChangeExtension(filepath, ".pdb"));

        static IDictionary<string, TypeDefinition> CacheTypes(string assemblyPath)
        {
            var paths = new List<string>();
            var resolver = new DefaultAssemblyResolver();
            var path = Path.GetDirectoryName(assemblyPath);
            paths.Add(path);
            resolver.AddSearchDirectory(path);
            var readsymbols = DoesPdbFileExist(assemblyPath);
            var readerParameters = new ReaderParameters { ReadSymbols = readsymbols, AssemblyResolver = resolver };
            var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);

            var types = new Dictionary<string, TypeDefinition>();

            foreach (var type in module.GetTypes())
            {
                var directory = Path.GetDirectoryName(type.Module.FullyQualifiedName);

                if (!paths.Contains(directory))
                {
                    resolver.AddSearchDirectory(directory);
                    paths.Add(directory);
                }

                types[type.FullName] = type;
            }

            return types;
        }

        static SequencePoint FirstOrDefaultSequencePoint(MethodDefinition testMethod)
        {
            CustomAttribute asyncStateMachineAttribute;

            if (TryGetAsyncStateMachineAttribute(testMethod, out asyncStateMachineAttribute))
            {
                testMethod = GetStateMachineMoveNextMethod(asyncStateMachineAttribute);
            }

            return FirstOrDefaultUnhiddenSequencePoint(testMethod.Body);
        }

        static bool TryGetAsyncStateMachineAttribute(MethodDefinition method, out CustomAttribute attribute)
        {
            attribute = method.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == "AsyncStateMachineAttribute");
            return attribute != null;
        }

        static MethodDefinition GetStateMachineMoveNextMethod(CustomAttribute asyncStateMachineAttribute)
        {
            var stateMachineType = (TypeDefinition)asyncStateMachineAttribute.ConstructorArguments[0].Value;
            var stateMachineMoveNextMethod = stateMachineType.GetMethods().First(m => m.Name == "MoveNext");
            return stateMachineMoveNextMethod;
        }

        static SequencePoint FirstOrDefaultUnhiddenSequencePoint(MethodBody body)
        {
            const int lineNumberIndicatingHiddenLine = 16707566; //0xfeefee

            foreach (var instruction in body.Instructions)
                if (instruction.SequencePoint != null && instruction.SequencePoint.StartLine != lineNumberIndicatingHiddenLine)
                    return instruction.SequencePoint;

            return null;
        }
    }
}
