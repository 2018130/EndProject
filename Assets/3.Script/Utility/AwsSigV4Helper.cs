using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Networking;

public static class AwsSigV4Helper
{
    public static void SignRequest(UnityWebRequest request, string region, string service, string accessKey, string secretKey, string sessionToken, string payload)
    {
        var uri = new Uri(request.url);
        string host = uri.Host;
        string path = uri.AbsolutePath;

        string method = request.method;
        string amzDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

        // 1. Canonical Request 생성 (요청을 정해진 규격으로 정리)
        string canonicalUri = path;
        string canonicalQueryString = uri.Query.TrimStart('?');

        // 헤더는 반드시 알파벳 소문자 순서대로 정렬해야 합니다 (host -> x-amz-date -> x-amz-security-token)
        string canonicalHeaders = $"host:{host}\nx-amz-date:{amzDate}\n";
        if (!string.IsNullOrEmpty(sessionToken))
        {
            canonicalHeaders += $"x-amz-security-token:{sessionToken}\n";
        }
        string signedHeaders = string.IsNullOrEmpty(sessionToken) ? "host;x-amz-date" : "host;x-amz-date;x-amz-security-token";
        string payloadHash = HexEncode(Hash(Encoding.UTF8.GetBytes(payload ?? "")));

        string canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        // 2. String to Sign 생성 (서명할 문자열 만들기)
        string credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        string stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{HexEncode(Hash(Encoding.UTF8.GetBytes(canonicalRequest)))}";

        // 3. 서명 키(Signature Key) 계산
        byte[] signingKey = GetSignatureKey(secretKey, dateStamp, region, service);
        byte[] signatureBytes = HmacSha256(stringToSign, signingKey);
        string signature = HexEncode(signatureBytes);

        // 4. 최종 Authorization 헤더 조립
        string authorizationHeader = $"AWS4-HMAC-SHA256 Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        // 5. UnityWebRequest에 헤더 세팅
        request.SetRequestHeader("host", host);
        request.SetRequestHeader("x-amz-date", amzDate);
        if (!string.IsNullOrEmpty(sessionToken))
        {
            request.SetRequestHeader("x-amz-security-token", sessionToken);
        }
        request.SetRequestHeader("Authorization", authorizationHeader);
    }

    private static byte[] Hash(byte[] bytes)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(bytes);
        }
    }

    private static byte[] HmacSha256(string data, byte[] key)
    {
        using (HMACSHA256 hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        byte[] kSecret = Encoding.UTF8.GetBytes("AWS4" + key);
        byte[] kDate = HmacSha256(dateStamp, kSecret);
        byte[] kRegion = HmacSha256(regionName, kDate);
        byte[] kService = HmacSha256(serviceName, kRegion);
        byte[] kSigning = HmacSha256("aws4_request", kService);
        return kSigning;
    }

    private static string HexEncode(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}