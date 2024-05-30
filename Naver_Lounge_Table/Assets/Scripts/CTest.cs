using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CTest : MonoBehaviour
    {
        private static CTest _instance;
        public static CTest Instance { get { return _instance; } }

        private List<string> m_strPanelName = null;
        private Dictionary<string, GameObject> m_ListPrefabs;
        public GameObject _VideoNode;
        private GameObject m_objCurrentObj;
    // Start is called before the first frame update
        void Start()
        {
            m_ListPrefabs = new Dictionary<string, GameObject>();
            m_strPanelName = new List<string>();
            LoadPrefabs("Video_Prefabs/", "01_GameObject");
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.A))
            {
                InsertMoviePanel(CConfigMng.Instance._strKR_IDLE);
            }
        }
        public void LoadPrefabs(string strFolderName, string strFileName)
        {
            GameObject tempObject = Resources.Load("Prefabs/" + strFolderName + strFileName) as GameObject;

            if (tempObject != null)
            {
                m_ListPrefabs.Add(strFileName, tempObject);
                m_strPanelName.Add(strFileName);
            }
        }
        public void InsertMoviePanel(string FilePath)
        {
            GameObject tempWindow;
            tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["01_GameObject"]) as GameObject;

            //tempWindow.GetComponentInChildren<CVideoPlayer>().InitiallizeContents(FilePath);
            tempWindow.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            tempWindow.transform.SetParent(_VideoNode.transform);
            RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
            rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);

            tempWindow.GetComponent<CUIPanel>().FadeInWindow();
            m_objCurrentObj = tempWindow;
        }
    }
}

