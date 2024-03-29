﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Security;
using SenseNet.Security.Tests.TestPortal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Security.Tests
{
    [TestClass]
    public class MembershipTests2
    {
        #region G1-G23: initial groups (well known when any test starting)
        int G1 = Id("G1"); int G2 = Id("G2"); int G3 = Id("G3"); int G4 = Id("G4"); int G5 = Id("G5");
        int G6 = Id("G6"); int G7 = Id("G7"); int G8 = Id("G8"); int G9 = Id("G9"); int G10 = Id("G10");
        int G11 = Id("G11"); int G12 = Id("G12"); int G13 = Id("G13"); int G14 = Id("G14"); int G15 = Id("G15");
        int G16 = Id("G16"); int G17 = Id("G17"); int G18 = Id("G18"); int G19 = Id("G19"); int G20 = Id("G20");
        int G21 = Id("G21"); int G22 = Id("G22"); int G23 = Id("G23");
        #endregion
        #region G30-G39: additional groups (for any test purposes)
        int G30 = Id("G30"); int G31 = Id("G31"); int G32 = Id("G32"); int G33 = Id("G33"); int G34 = Id("G34");
        int G35 = Id("G35"); int G36 = Id("G36"); int G37 = Id("G37"); int G38 = Id("G38"); int G39 = Id("G39");
        #endregion
        #region U1-U38: initial users (well known when any test starting)
        int U1 = Id("U1"); int U2 = Id("U2"); int U3 = Id("U3"); int U4 = Id("U4"); int U5 = Id("U5");
        int U6 = Id("U6"); int U7 = Id("U7"); int U8 = Id("U8"); int U9 = Id("U9"); int U10 = Id("U10");
        int U11 = Id("U11"); int U12 = Id("U12"); int U13 = Id("U13"); int U14 = Id("U14"); int U15 = Id("U15");
        int U16 = Id("U16"); int U17 = Id("U17"); int U18 = Id("U18"); int U19 = Id("U19"); int U20 = Id("U20");
        int U21 = Id("U21"); int U22 = Id("U22"); int U23 = Id("U23"); int U24 = Id("U24"); int U25 = Id("U25");
        int U26 = Id("U26"); int U27 = Id("U27"); int U28 = Id("U28"); int U29 = Id("U29"); int U30 = Id("U30");
        int U31 = Id("U31"); int U32 = Id("U32"); int U33 = Id("U33"); int U34 = Id("U34"); int U35 = Id("U35");
        int U36 = Id("U36"); int U37 = Id("U37"); int U38 = Id("U38");
        #endregion
        #region U40-U49: additional users (for any test purposes)
        int U40 = Id("U40"); int U41 = Id("U41"); int U42 = Id("U42"); int U43 = Id("U43"); int U44 = Id("U44");
        int U45 = Id("U45"); int U46 = Id("U46"); int U47 = Id("U47"); int U48 = Id("U48"); int U49 = Id("U49");
        #endregion
        #region initial membership
        readonly string InitialMembership = "U1:G1,G2|U2:G1,G2|U3:G1,G4|U4:G1,G4|U5:G1,G4|U6:G1,G6|U7:G1,G3,G8|U8:G1,G3,G8|U9:G1,G3,G8|U10:G1,G3|" +
                "U11:G1,G3,G10|U12:G1,G3,G11|U13:G1,G3,G11|U14:G1,G5,G12|U15:G1,G5,G12|U16:G1,G5,G12|U17:G1,G5,G12|U18:G1,G5,G12|" +
                "U19:G1,G5,G12|U20:G1,G5,G12|U21:G1,G5,G13|U22:G1,G5,G14|U23:G1,G5,G14|U24:G1,G5,G15|U25:G1,G3,G9,G16|U26:G1,G3,G9,G17|" +
                "U27:G1,G3,G9,G18|U28:G1,G3,G9,G18|U29:G1,G3,G9,G19|U30:G20|U31:G20|U32:G20|U33:G20|U34:G20,G21|U35:G20,G21|U36:G20,G22|" +
                "U37:G20,G23|U38:G20,G23";
        #endregion

        Context context;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void StartTest()
        {
            context = Tools.GetEmptyContext(TestUser.User1);
            Tools.InitializeInMemoryMembershipStorage(@"G1:G2,G3,G4,G5,G6|G2:U1,U2|G3:U10,G7,G8,G9,G10,G11|G4:U3,U4,U5|G5:G12,G13,G14,G15|
                G6:U6|G7:|G8:U7,U8,U9|G9:G16,G17,G18,G19|G10:U11|G11:U12,U13|G12:U14,U15,U16,U17,U18,U19,U20|G13:U21|G14:U22,U23|
                G15:U24|G16:U25|G17:U26|G18:U27,U28|G19:U29|G20:U30,U31,U32,U33,G21,G22,G23|G21:U34,U35|G22:U36|G23:U37,U38");
            context.Security.Cache.Reset(context.Security.DataProvider);
            Assert.AreEqual(InitialMembership, DumpMembership(context.Security));
        }
        [TestCleanup]
        public void Finishtest()
        {
            Tools.CheckIntegrity(TestContext.TestName, context.Security);
        }

        /*==================================================================================*/

        [TestMethod]
        public void Membership2_MakeCircleWithNewGroup()
        {
            var ctx = context.Security;

            // operation
            ctx.AddMembersToSecurityGroup(G30, null, new[] { G9 }, new[] { G18 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U25, G18, G30)
                .AddGroupsToUser(U26, G18, G30)
                .AddGroupsToUser(U27, G18, G30)
                .AddGroupsToUser(U28, G18, G30)
                .AddGroupsToUser(U29, G18, G30)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_MakeCircleWithNewGroupAndUsers()
        {
            var ctx = context.Security;

            // operation
            ctx.AddMembersToSecurityGroup(G30, new[] { U1, U40 }, new[] { G9 }, new[] { G18 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U1, G3, G9, G18, G30)
                .AddGroupsToUser(U25, G18, G30)
                .AddGroupsToUser(U26, G18, G30)
                .AddGroupsToUser(U27, G18, G30)
                .AddGroupsToUser(U28, G18, G30)
                .AddGroupsToUser(U29, G18, G30)
                .AddGroupsToUser(U40, G1, G3, G9, G18, G30)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_AddExistingUserToMorethanOneGroup()
        {
            var ctx = context.Security;

            // operation
            ctx.AddUserToSecurityGroups(U1, new[] { G4, G7, G10, G20, G22 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U1, G3, G4, G7, G10, G20, G22)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_AddNewUserToMorethanOneGroup()
        {
            var ctx = context.Security;

            // operation
            ctx.AddUserToSecurityGroups(U40, new[] { G4, G7, G10, G20, G22 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U40, G1, G3, G4, G7, G10, G20, G22)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_RemoveUserFromMorethanOneGroup()
        {
            var ctx = context.Security;
            ctx.AddUserToSecurityGroups(U1, new[] { G4, G7, G10, G20, G22 });
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U1, G3, G4, G7, G10, G20, G22)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));

            // operation
            ctx.RemoveUserFromSecurityGroups(U1, new[] { G4, G7, G10, G20, G22 });

            // test
            Assert.AreEqual(InitialMembership, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_AddNewUserToEmptyGroup()
        {
            var ctx = context.Security;

            // operation
            ctx.AddUsersToSecurityGroup(G7, new[] { U40 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U40, G1, G3, G7)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteGroupFromCircle()
        {
            var ctx = context.Security;

            // preparation (make circle with a new group and an existing and a new user).
            ctx.AddMembersToSecurityGroup(G30, new[] { U1, U40 }, new[] { G9 }, new[] { G18 });
            Assert.AreEqual("U1:G1,G2,G3,G9,G18,G30|U2:G1,G2|U3:G1,G4|U4:G1,G4|U5:G1,G4|U6:G1,G6|U7:G1,G3,G8|U8:G1,G3,G8|U9:G1,G3,G8|U10:G1,G3|" +
                "U11:G1,G3,G10|U12:G1,G3,G11|U13:G1,G3,G11|U14:G1,G5,G12|U15:G1,G5,G12|U16:G1,G5,G12|U17:G1,G5,G12|U18:G1,G5,G12|" +
                "U19:G1,G5,G12|U20:G1,G5,G12|U21:G1,G5,G13|U22:G1,G5,G14|U23:G1,G5,G14|U24:G1,G5,G15|U25:G1,G3,G9,G16,G18,G30|U26:G1,G3,G9,G17,G18,G30|" +
                "U27:G1,G3,G9,G18,G30|U28:G1,G3,G9,G18,G30|U29:G1,G3,G9,G18,G19,G30|U30:G20|U31:G20|U32:G20|U33:G20|U34:G20,G21|U35:G20,G21|U36:G20,G22|" +
                "U37:G20,G23|U38:G20,G23|U40:G1,G3,G9,G18,G30", DumpMembership(ctx));

            // operation
            ctx.DeleteSecurityGroup(G30);

            // test
            Assert.AreEqual(InitialMembership, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteGroup_RootNode()
        {
            var ctx = context.Security;

            // operation
            ctx.DeleteSecurityGroup(G20);

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U30, U31, U32, U33)
                .RemoveGroupsFromUser(U34, G20)
                .RemoveGroupsFromUser(U35, G20)
                .RemoveGroupsFromUser(U36, G20)
                .RemoveGroupsFromUser(U37, G20)
                .RemoveGroupsFromUser(U38, G20)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteGroup_TreeNode()
        {
            var ctx = context.Security;

            // operation
            ctx.DeleteSecurityGroup(G3);

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U10)
                .RemoveGroupsFromUser(U7, G1, G3)
                .RemoveGroupsFromUser(U8, G1, G3)
                .RemoveGroupsFromUser(U9, G1, G3)
                .RemoveGroupsFromUser(U11, G1, G3)
                .RemoveGroupsFromUser(U12, G1, G3)
                .RemoveGroupsFromUser(U13, G1, G3)
                .RemoveGroupsFromUser(U25, G1, G3)
                .RemoveGroupsFromUser(U26, G1, G3)
                .RemoveGroupsFromUser(U27, G1, G3)
                .RemoveGroupsFromUser(U28, G1, G3)
                .RemoveGroupsFromUser(U29, G1, G3)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteGroup_Leaf()
        {
            var ctx = context.Security;

            // operation
            ctx.DeleteSecurityGroup(G8);

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U7, U8, U9)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_RemoveUsersAndGroupsFromRoot()
        {
            var ctx = context.Security;

            // operation
            ctx.RemoveMembersFromSecurityGroup(G20, new[] { U31, U33 }, new[] { G21 });

            // test
            var expected = new MembershipEditor(InitialMembership)
            .DeleteUsers(U31, U33)
            .RemoveGroupsFromUser(U34, G20)
            .RemoveGroupsFromUser(U35, G20)
            .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_RemoveUsersAndGroups()
        {
            var ctx = context.Security;

            // operation
            ctx.RemoveMembersFromSecurityGroup(G3, new[] { U10 }, new[] { G9 }, new[] { G1 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U10)
                .RemoveGroupFromUsers(G1, U7, U8, U9, U11, U12, U13, U25, U26, U27, U28, U29)
                .RemoveGroupFromUsers(G3, U25, U26, U27, U28, U29)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteUser()
        {
            var ctx = context.Security;

            // operation
            ctx.DeleteUser(U1);

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U1)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_DeleteUser_MoreInstance()
        {
            var ctx = context.Security;
            ctx.AddUserToSecurityGroups(U1, new[] { G4, G7, G10, G20, G22 });
            var expected = new MembershipEditor(InitialMembership)
                .AddGroupsToUser(U1, G3, G4, G7, G10, G20, G22)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));

            // operation
            ctx.DeleteUser(U1);

            // test
            expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U1)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }


        [TestMethod]
        public void Membership2_DeleteUsersAndGroups()
        {
            var ctx = context.Security;

            // operation
            ctx.DeleteIdentities(new[] { U1, G10, G20, U26 });

            // test
            var expected = new MembershipEditor(InitialMembership)
                .DeleteUsers(U1, U11, U26, U30, U31, U32, U33)
                .RemoveGroupFromUsers(G20, U34, U35, U36, U37, U38)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }


        [TestMethod]
        public void Membership2_DeleteGroup_Complex()
        {
            var ctx = context.Security;

            // preparation
            ctx.AddUserToSecurityGroups(U1, new[] { G5, G18, G20, G23 });
            ctx.AddUserToSecurityGroups(U40, new[] { G20 });
            ctx.AddMembersToSecurityGroup(G20, null, null, new[] { G10, G12 });
            var membershipBefore = DumpMembership(ctx);

            // operation
            ctx.DeleteSecurityGroup(G20);

            // test
            var expected = new MembershipEditor(membershipBefore)
                .RemoveGroupsFromUser(U1, G10, G12, G20)
                .RemoveGroupsFromUser(U34, G1, G3, G5, G10, G12, G20)
                .RemoveGroupsFromUser(U35, G1, G3, G5, G10, G12, G20)
                .RemoveGroupsFromUser(U36, G1, G3, G5, G10, G12, G20)
                .RemoveGroupsFromUser(U37, G1, G3, G5, G10, G12, G20)
                .RemoveGroupsFromUser(U38, G1, G3, G5, G10, G12, G20)
                .DeleteUsers(U30, U31, U32, U33, U40)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_RemoveGroupMember_Complex()
        {
            var ctx = context.Security;

            // preparation
            ctx.AddUserToSecurityGroups(U1, new[] { G5, G18, G20, G23 });
            ctx.AddUserToSecurityGroups(U40, new[] { G20 });
            ctx.AddMembersToSecurityGroup(G20, null, null, new[] { G10, G12 });
            var membershipBefore = DumpMembership(ctx);

            // operation
            ctx.RemoveMembersFromSecurityGroup(G20, null, null, new[] { G10, G12 });

            // test
            var expected = new MembershipEditor(membershipBefore)
                .RemoveGroupsFromUser(U1, G10, G12)
                .RemoveGroupsFromUser(U30, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U31, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U32, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U33, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U34, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U35, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U36, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U37, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U38, G1, G3, G5, G10, G12)
                .RemoveGroupsFromUser(U40, G1, G3, G5, G10, G12)
                .ToString();
            Assert.AreEqual(expected, DumpMembership(ctx));
        }

        [TestMethod]
        public void Membership2_IsInGroup()
        {
            var ctx = context.Security;

            // user in group
            Assert.IsTrue(ctx.IsInGroup(U13, G11)); // direct parent
            Assert.IsTrue(ctx.IsInGroup(U13, G3));  // parent of parent (transitive)
            Assert.IsFalse(ctx.IsInGroup(U13, G4)); // unrelated

            // group in group
            Assert.IsTrue(ctx.IsInGroup(G11, G3));  // direct parent
            Assert.IsTrue(ctx.IsInGroup(G11, G1));  // parent of parent (transitive)
            Assert.IsFalse(ctx.IsInGroup(G11, G4)); // unrelated

            ctx.DeleteSecurityGroup(G3);

            // user in group
            Assert.IsTrue(ctx.IsInGroup(U13, G11)); // direct parent
            Assert.IsFalse(ctx.IsInGroup(U13, G3)); // parent of parent (transitive)
            Assert.IsFalse(ctx.IsInGroup(U13, G4)); // unrelated

            // group in group
            Assert.IsFalse(ctx.IsInGroup(G11, G3)); // direct parent
            Assert.IsFalse(ctx.IsInGroup(G11, G1)); // parent of parent (transitive)
            Assert.IsFalse(ctx.IsInGroup(G11, G4)); // unrelated
        }

        [TestMethod]
        public void Membership2_IsInGroup_Self()
        {
            var ctx = context.Security;

            // not a member of itself
            Assert.IsFalse(ctx.IsInGroup(U13, U13)); // user in user
            Assert.IsFalse(ctx.IsInGroup(G3, G3));   // group in itself
        }

        /*==================================================================================*/

        internal static string DumpMembership(SecurityContext context)
        {
            return DumpMembership(context.Cache.Membership);
        }
        public static string DumpMembership(Dictionary<int, List<int>> membership)
        {
            if (membership.Count == 0)
                return String.Empty;

            var sb = new StringBuilder();
            foreach (var userId in membership.Keys.OrderBy(s => s))
            {
                sb.Append(GetUserName(userId)).Append(":");
                sb.Append(String.Join(",", membership[userId].OrderBy(s => s).Select(g => GetGroupName(g))));
                sb.Append("|");
            }
            sb.Length--;
            return sb.ToString();
        }

        private static string GetUserName(int userId)
        {
            return "U" + (userId % 100);
        }
        private static string GetGroupName(int groupId)
        {
            return "G" + (groupId % 100);
        }

        private static int Id(string name)
        {
            return Tools.GetId(name);
        }

        private class MembershipEditor
        {
            Dictionary<int, List<int>> Membership = new Dictionary<int, List<int>>();

            public MembershipEditor(string initialState)
            {
                foreach (var userRecord in initialState.Split('|'))
                {
                    var s = userRecord.Split(':');
                    var userId = Id(s[0]);
                    Membership[userId] = s[1].Split(',').Select(g => Id(g)).ToList();
                }
            }

            public override string ToString()
            {
                return DumpMembership(this.Membership);
            }

            //-------------------------------------------------------------------------------------

            public MembershipEditor AddGroupsToUser(int userId, params int[] groupIds)
            {
                List<int> user;
                if (!Membership.TryGetValue(userId, out user))
                {
                    Membership[userId] = groupIds.Distinct().ToList();
                }
                else
                {
                    foreach (var groupId in groupIds)
                        if (!user.Contains(groupId))
                            user.Add(groupId);
                }
                return this;
            }

            internal MembershipEditor DeleteUsers(params int[] userIds)
            {
                foreach (var userId in userIds)
                    Membership.Remove(userId);
                return this;
            }

            internal MembershipEditor RemoveGroupsFromUser(int userId, params int[] groupIds)
            {
                List<int> user;
                if (Membership.TryGetValue(userId, out user))
                    user.RemoveAll(x => groupIds.Contains(x));
                return this;
            }

            internal MembershipEditor RemoveGroupFromUsers(int groupId, params int[] userIds)
            {
                foreach (var userId in userIds)
                    RemoveGroupsFromUser(userId, groupId);
                return this;
            }
        }
    }
}
