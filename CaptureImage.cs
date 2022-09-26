using UnityEngine;
using System;
using System.Net;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.WebCam;
using TMPro;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

public class CaptureImage : MonoBehaviour

{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    public Transform cam;
    public string photoFileName = null;
    public GameObject quad;
    public Renderer quadRenderer;
    public Texture2D texture = null;
    private string MIXED_REALITY_DEVICE_PORTAL_PASSWORD = "password";
    private string MIXED_REALITY_DEVICE_PORTAL_USERNAME = "username";
    private string HOLOLENS2_LOCAL_IP_ADDR = "192.168.0.114";

    // Use this for initialization
    [Serializable]
    public class Picture
    {
        public string PhotoFileName;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
    void Start()
    {       
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        quad.transform.parent = this.transform;
        resetScreenshotPosition();
        StartCoroutine(GetScreenshotFromHololens());
    }



    private string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    IEnumerator GetScreenshotFromHololens()
    {
        
        UnityWebRequest req = UnityWebRequest.Post("http://"+MIXED_REALITY_DEVICE_PORTAL_USERNAME+":"+MIXED_REALITY_DEVICE_PORTAL_PASSWORD+"@"+HOLOLENS2_LOCAL_IP_ADDR + "/api/holographic/mrc/photo?holo=false&pv=true", "");
        req.certificateHandler = new BypassCertificate();
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            if (req.isNetworkError || req.isHttpError || req.isNetworkError)
                print("Error: " + req.error);
        }
        else
        {
            Picture picture = JsonUtility.FromJson<Picture>(req.downloadHandler.text);
            photoFileName = picture.PhotoFileName;
        }
       
        UnityWebRequest req2 = UnityWebRequestTexture.GetTexture("http://"+MIXED_REALITY_DEVICE_PORTAL_USERNAME+":"+MIXED_REALITY_DEVICE_PORTAL_PASSWORD+"@"+HOLOLENS2_LOCAL_IP_ADDR + "/api/holographic/mrc/file?filename=" + Base64Encode(photoFileName));
        req2.certificateHandler = new BypassCertificate();
        yield return req2.SendWebRequest();
        if (req2.result != UnityWebRequest.Result.Success)
        {
            if (req2.isNetworkError || req2.isHttpError || req2.isNetworkError)
                print("Error: " + req2.error);
            Debug.Log(req2.downloadHandler.text);
        }
        else
        {
            resetScreenshotPosition();
            texture = DownloadHandlerTexture.GetContent(req2);
            quadRenderer.material.SetTexture("_MainTex", texture);
        }
       
        yield return new WaitForSeconds(5);
        StartCoroutine(GetScreenshotFromHololens());
    }

    void resetScreenshotPosition(){
        Vector3 forwardPosition = Camera.main.transform.rotation * Vector3.forward*3;
        Vector3 finalPosition = Camera.main.transform.position + forwardPosition;
        quad.transform.localPosition = finalPosition;
        quad.transform.rotation = Camera.main.transform.rotation;
    }
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
