namespace DBH.UnitTest.UnitTests;

public class EvaluateTransactionAsync_DifferentIdentity_SwitchesActiveState
{
    [Fact]
    public async Task EvaluateTransactionAsync_DifferentIdentity_SwitchesActiveState_Test()
    {
        var resolver = new FabricGatewayIdentitySwitchingTestSupport.SequenceIdentityResolver(
            FabricGatewayIdentitySwitchingTestSupport.CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"),
            FabricGatewayIdentitySwitchingTestSupport.CreateIdentity("org-B", "Hospital2MSP", "peer0.hospital2.ehr.com:9051", "peer0.hospital2.ehr.com"));

        await using var sut = FabricGatewayIdentitySwitchingTestSupport.CreateGatewayClient(resolver);

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-1");
        var firstIdentity = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeIdentityKey");
        var firstMsp = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeMspId");
        var firstPeer = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activePeerEndpoint");

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-2");
        var secondIdentity = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeIdentityKey");
        var secondMsp = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeMspId");
        var secondPeer = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activePeerEndpoint");

        Assert.Equal("org-A", firstIdentity);
        Assert.Equal("Hospital1MSP", firstMsp);
        Assert.Equal("peer0.hospital1.ehr.com:7051", firstPeer);
        Assert.Equal("org-B", secondIdentity);
        Assert.Equal("Hospital2MSP", secondMsp);
        Assert.Equal("peer0.hospital2.ehr.com:9051", secondPeer);
    }
}