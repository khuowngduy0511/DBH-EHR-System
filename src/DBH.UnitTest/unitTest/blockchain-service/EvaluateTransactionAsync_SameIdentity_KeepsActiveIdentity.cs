namespace DBH.UnitTest.UnitTests;

public class EvaluateTransactionAsync_SameIdentity_KeepsActiveIdentity
{
    [Fact]
    public async Task EvaluateTransactionAsync_SameIdentity_KeepsActiveIdentity_Test()
    {
        var resolver = new FabricGatewayIdentitySwitchingTestSupport.SequenceIdentityResolver(
            FabricGatewayIdentitySwitchingTestSupport.CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"),
            FabricGatewayIdentitySwitchingTestSupport.CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"));

        await using var sut = FabricGatewayIdentitySwitchingTestSupport.CreateGatewayClient(resolver);

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-1");
        var firstIdentity = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeIdentityKey");

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-2");
        var secondIdentity = FabricGatewayIdentitySwitchingTestSupport.GetPrivateField<string>(sut, "_activeIdentityKey");

        Assert.Equal("org-A", firstIdentity);
        Assert.Equal("org-A", secondIdentity);
    }
}