using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;

namespace Cashier.Commons;

public class TeamHelper
{
    public List<TeamMember> TeamList = [];

    public unsafe void UpdateTeamList()
    {
        this.TeamList = [];

        var groupManager = GroupManager.Instance();
        if (groupManager->MemberCount > 0) {
            this.AddMembersFromGroupManager(groupManager);
        } else {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0) {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupArraySpan[localIndex], true);

                for (var i = 0; i < cwProxy->CrossRealmGroupArraySpan.Length; i++) {
                    if (i == localIndex) {
                        continue;
                    }

                    this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupArraySpan[i]);
                }
            }
        }

        // Add self if not in party
        if (this.TeamList.Count == 0 && Svc.ClientState.LocalPlayer != null) {
            var selfName = Svc.ClientState.LocalPlayer.Name.TextValue;
            var selfWorldId = Svc.ClientState.LocalPlayer.HomeWorld.Id;
            var selfJobId = Svc.ClientState.LocalPlayer.ClassJob.Id;
            this.AddTeamMember(Svc.ClientState.LocalPlayer.ObjectId, selfName, (ushort)selfWorldId, true);
        }
    }

    private unsafe void AddMembersFromCRGroup(CrossRealmGroup crossRealmGroup, bool isLocalPlayerGroup = false)
    {
        for (var i = 0; i < crossRealmGroup.GroupMemberCount; i++) {
            var groupMember = crossRealmGroup.GroupMembersSpan[i];
            this.AddTeamMember(groupMember.ObjectId, Utils.ReadSeString(groupMember.Name).TextValue, (ushort)groupMember.HomeWorld, isLocalPlayerGroup);
        }
    }

    private unsafe void AddMembersFromGroupManager(GroupManager* groupManager)
    {
        var partyMemberList = AgentModule.Instance()->GetAgentHUD()->PartyMemberListSpan;
        var groupManagerIndexLeft = Enumerable.Range(0, groupManager->MemberCount).ToList();

        for (var i = 0; i < groupManager->MemberCount; i++) {
            var hudPartyMember = partyMemberList[i];
            var hudPartyMemberNameRaw = hudPartyMember.Name;
            if (hudPartyMemberNameRaw != null) {
                var hudPartyMemberName = Utils.ReadSeString(hudPartyMemberNameRaw).TextValue;
                for (var j = 0; j < groupManager->MemberCount; j++) {
                    // handle duplicate names from different worlds
                    if (!groupManagerIndexLeft.Contains(j)) {
                        continue;
                    }

                    var partyMember = groupManager->GetPartyMemberByIndex(j);
                    if (partyMember != null) {
                        var partyMemberName = Utils.ReadSeString(partyMember->Name).TextValue;
                        if (hudPartyMemberName.Equals(partyMemberName)) {
                            this.AddTeamMember(partyMember->ObjectID, partyMemberName, partyMember->HomeWorld, true);
                            groupManagerIndexLeft.Remove(j);
                            break;
                        }
                    }
                }
            }
        }

        for (var i = 0; i < 20; i++) {
            var allianceMember = groupManager->GetAllianceMemberByIndex(i);
            if (allianceMember != null) {
                this.AddTeamMember(allianceMember->ObjectID, Utils.ReadSeString(allianceMember->Name).TextValue, allianceMember->HomeWorld, false);
            }
        }
    }

    private void AddTeamMember(uint objectId, string fullName, ushort worldId, bool isInParty)
    {
        var world = Svc.DataManager.GetExcelSheet<World>()?.FirstOrDefault(x => x.RowId == worldId);
        if (world is not { IsPublic: true }) {
            return;
        }

        if (fullName == string.Empty) {
            return;
        }

        /*var splitName = fullName.Split(' ');
        if (splitName.Length != 2)
        {
            return;
        }*/

        this.TeamList.Add(new TeamMember { ObjectId = objectId, FirstName = fullName, World = world.Name, IsInParty = isInParty });
    }

    public class TeamMember
    {
        public uint ObjectId;
        public string FirstName = null!;
        public string World = null!;
        public bool IsInParty;
    }
}

