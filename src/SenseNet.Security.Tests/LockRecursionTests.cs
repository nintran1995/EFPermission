﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Security.Tests.TestPortal;

namespace SenseNet.Security.Tests
{
    [TestClass]
    public class LockRecursionTests
    {
        internal class LockRecursionUser : ISecurityUser
        {
            private Func<int, IEnumerable<int>> _extenderMethod;
            public int Id { get; }

            public LockRecursionUser(int id, Func<int, IEnumerable<int>> extenderMethod)
            {
                Id = id;
                _extenderMethod = extenderMethod;
            }
            public IEnumerable<int> GetDynamicGroups(int entityId)
            {
                return _extenderMethod(entityId);
            }
        }

        #region Infrastructure 
        private Context GetContextAndStartTheSystem(LockRecursionUser currentUser)
        {
            SecurityActivityQueue._setCurrentExecutionState(new CompletionState());
            MemoryDataProvider.LastActivityId = 0;
            Context.StartTheSystem(new MemoryDataProvider(DatabaseStorage.CreateEmpty()), new DefaultMessageProvider(), null);
            var context = new Context(currentUser);
            CreatePlayground(context);
            return context;
        }
        private Context GetContext(LockRecursionUser currentUser)
        {
            SecurityActivityQueue._setCurrentExecutionState(new CompletionState());
            MemoryDataProvider.LastActivityId = 0;
            return new Context(currentUser);
        }

        private Dictionary<int, TestEntity> _repository = new Dictionary<int, TestEntity>();

        private void CreatePlayground(Context context)
        {
            TestEntity e;
            var u1 = TestUser.User1;

            CreateEntity(context, "E1", null, u1);
            {
                CreateEntity(context, "E2", "E1", u1); // +U1:____+, +U2:____+, +U3:____+, +G1:____+
                {
                    CreateEntity(context, "E5", "E2", u1); // +U3___+, +G2:_+__
                    {
                        CreateEntity(context, "E14", "E5", u1);
                        {
                            CreateEntity(context, "E50", "E14", u1);
                            {
                                CreateEntity(context, "E51", "E50", u1);
                                {
                                    CreateEntity(context, "E52", "E51", u1);
                                }
                                CreateEntity(context, "E53", "E50", u1);
                            }
                        }
                        CreateEntity(context, "E15", "E5", u1); // +U1:__+_, +G2:__+_
                    }
                    CreateEntity(context, "E6", "E2", u1);
                    {
                        CreateEntity(context, "E16", "E6", u1); // +U2:__+_
                        CreateEntity(context, "E17", "E6", u1);
                    }
                    CreateEntity(context, "E7", "E2", u1);
                    {
                        CreateEntity(context, "E18", "E7", u1);
                        CreateEntity(context, "E19", "E7", u1);
                    }
                }
                CreateEntity(context, "E3", "E1", u1);
                {
                    CreateEntity(context, "E8", "E3", u1);
                    {
                        CreateEntity(context, "E20", "E8", u1); // +U1:_+__
                        CreateEntity(context, "E21", "E8", u1);
                        {
                            CreateEntity(context, "E22", "E21", u1); // BREAK
                            CreateEntity(context, "E23", "E21", u1);
                            CreateEntity(context, "E24", "E21", u1);
                            CreateEntity(context, "E25", "E21", u1); // +U2:___+
                            CreateEntity(context, "E26", "E21", u1);
                            CreateEntity(context, "E27", "E21", u1);
                            CreateEntity(context, "E28", "E21", u1); // +U1:___+, +G1:__+_
                            CreateEntity(context, "E29", "E21", u1);
                        }
                    }
                    CreateEntity(context, "E9", "E3", u1);
                    CreateEntity(context, "E10", "E3", u1); // +G2:__+_
                }
                CreateEntity(context, "E4", "E1", u1); // +U3:___-, +G1:__+_
                {
                    CreateEntity(context, "E11", "E4", u1); // +G2:__+_
                    CreateEntity(context, "E12", "E4", u1); // +U1:_+__
                    {
                        CreateEntity(context, "E30", "E12", u1);
                        {
                            CreateEntity(context, "E31", "E30", u1);
                            {
                                CreateEntity(context, "E33", "E31", u1);
                                CreateEntity(context, "E34", "E31", u1); // BREAK +U1:_+__, +U2:___+, +U3:___-, +G1:__+_
                                {
                                    CreateEntity(context, "E40", "E34", u1);
                                    CreateEntity(context, "E43", "E34", u1);
                                    {
                                        CreateEntity(context, "E44", "E43", u1);
                                        CreateEntity(context, "E45", "E43", u1);
                                        CreateEntity(context, "E46", "E43", u1);
                                        CreateEntity(context, "E47", "E43", u1);
                                        CreateEntity(context, "E48", "E43", u1);
                                        CreateEntity(context, "E49", "E43", u1);
                                    }
                                }
                            }
                            CreateEntity(context, "E32", "E30", u1); // +U1:___+
                            {
                                CreateEntity(context, "E35", "E32", u1); // BREAK +U2:___+, +U3:___-, +G1:__+_
                                {
                                    CreateEntity(context, "E41", "E35", u1); // BREAK
                                    {
                                        CreateEntity(context, "E42", "E41", u1);
                                    }
                                }
                                CreateEntity(context, "E36", "E32", u1); // BREAK +U3:___-, +G1:__+_
                                {
                                    CreateEntity(context, "E37", "E36", u1); // BREAK +U3:___-, +G1:__+_
                                    {
                                        CreateEntity(context, "E38", "E37", u1);
                                        CreateEntity(context, "E39", "E37", u1);
                                    }
                                }
                            }
                        }
                    }
                    CreateEntity(context, "E13", "E4", u1);
                }
            }

            var ctx = context.Security;

            ctx.AddUsersToSecurityGroup(Id("G13"), new[] { Id("U13") });
            ctx.AddUsersToSecurityGroup(Id("G12"), new[] { Id("U12") });
            ctx.AddUsersToSecurityGroup(Id("G11"), new[] { Id("U11") });
            ctx.AddUsersToSecurityGroup(Id("G10"), new[] { Id("U10") });
            ctx.AddGroupToSecurityGroups(Id("G11"), new[] { Id("G10") });
            ctx.AddGroupToSecurityGroups(Id("G13"), new[] { Id("G11") });
            ctx.AddGroupToSecurityGroups(Id("G13"), new[] { Id("G12") });

            ctx.CreateAclEditor()
                .Allow(Id("E2"), Id("U1"), false, PermissionType.FullControl)
                .Allow(Id("E2"), Id("U2"), false, PermissionType.FullControl)
                .Allow(Id("E2"), Id("U3"), false, PermissionType.FullControl)
                .Allow(Id("E2"), Id("G1"), false, PermissionType.FullControl)

                .Deny(Id("E4"), Id("U3"), false, PermissionType.FullControl)
                .Allow(Id("E4"), Id("G1"), false, PermissionType.FullControl)

                .Allow(Id("E5"), Id("U3"), false, PermissionType.FullControl)
                .Allow(Id("E5"), Id("G2"), false, PermissionType.FullControl)

                .Allow(Id("E10"), Id("G2"), false, PermissionType.FullControl)
                .Allow(Id("E11"), Id("G2"), false, PermissionType.FullControl)
                .Allow(Id("E12"), Id("U1"), false, PermissionType.FullControl)

                .Allow(Id("E15"), Id("U1"), false, PermissionType.FullControl)
                .Allow(Id("E15"), Id("G2"), false, PermissionType.FullControl)
                .Allow(Id("E16"), Id("U2"), false, PermissionType.FullControl)
                .Allow(Id("E20"), Id("U1"), false, PermissionType.FullControl)

                .Allow(Id("E25"), Id("U2"), false, PermissionType.FullControl)

                .Allow(Id("E28"), Id("U1"), false, PermissionType.FullControl)
                .Allow(Id("E28"), Id("G1"), false, PermissionType.FullControl)

                .Allow(Id("E32"), Id("U1"), false, PermissionType.FullControl)

                .Apply();

            ctx.CreateAclEditor()
                .BreakInheritance(Id("E22"), new[] { EntryType.Normal })

                .BreakInheritance(Id("E34"), new[] { EntryType.Normal })
                .Allow(Id("E34"), Id("U2"), false, PermissionType.FullControl)
                .BreakInheritance(Id("E35"), new[] { EntryType.Normal })
                .Allow(Id("E35"), Id("U2"), false, PermissionType.FullControl)
                .ClearPermission(Id("E35"), Id("U1"), false, PermissionType.FullControl)

                .BreakInheritance(Id("E36"), new[] { EntryType.Normal })
                .ClearPermission(Id("E36"), Id("U1"), false, PermissionType.FullControl)


                .Apply();

            ctx.CreateAclEditor()
                .BreakInheritance(Id("E37"), new[] { EntryType.Normal })

                // E41 and her subtree (E41, E42) is disabled for everyone except the system user
                .BreakInheritance(Id("E41"), new EntryType[0])

                .Apply();

        }

        private void CreateEntity(Context context, string name, string parentName, TestUser owner)
        {
            var entity = new TestEntity
            {
                Id = Id(name),
                Name = name,
                OwnerId = owner == null ? default(int) : owner.Id,
                Parent = parentName == null ? null : _repository[Id(parentName)],
            };
            _repository.Add(entity.Id, entity);
            context.Security.CreateSecurityEntity(entity);
        }

        private int Id(string name)
        {
            return Tools.GetId(name);
        }
        #endregion

        //[TestMethod]
        public void LockRecursion_NoExtension()
        {
            var user = new LockRecursionUser(Id("E1"), entityId =>
            {
                // There is no extension.
                return new int[0];
            });

            var context = GetContextAndStartTheSystem(user);
            context.Security.HasPermission(Id("E1"), PermissionType.FullControl);
        }

        //[TestMethod]
        public void LockRecursion_AvoidWithElevation()
        {
            var user = new LockRecursionUser(Id("E1"), entityId =>
            {
                // Simulate permission check in the getter of the PortalContext.ContextNode 
                var elevatedContext = GetContext(new LockRecursionUser(-1, e => new int[0]));
                var aces = elevatedContext.Security.GetEffectiveEntries(Id("E1"), new[] {Id("U1")});
                return new int[0];
            });

            var context = GetContextAndStartTheSystem(user);
            context.Security.HasPermission(Id("E1"), PermissionType.FullControl);
        }
    }
}
