using NUnit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

//===============================================================================================================================================================
/*      ,------.   ,---. ,--------.,--.  ,--.     ,----.   ,------.,--.  ,--.,------.,------.   ,---. ,--------. ,-----. ,------.  
        |  .--. ' /  O  \'--.  .--'|  '--'  |    '  .-./   |  .---'|  ,'.|  ||  .---'|  .--. ' /  O  \'--.  .--''  .-.  '|  .--. ' 
        |  '--' ||  .-.  |  |  |   |  .--.  |    |  | .---.|  `--, |  |' '  ||  `--, |  '--'.'|  .-.  |  |  |   |  | |  ||  '--'.' 
        |  | --' |  | |  |  |  |   |  |  |  |    '  '--'  ||  `---.|  | `   ||  `---.|  |\  \ |  | |  |  |  |   '  '-'  '|  |\  \  
        `--'     `--' `--'  `--'   `--'  `--'     `------' `------'`--'  `--'`------'`--' '--'`--' `--'  `--'    `-----' `--' '--'                             */
//===============================================================================================================================================================
//
//  PATH GENERATOR GUI CLASS
//
//  GUI Script to help generate path based on Bézier curve 
//  베지어 곡선 기반의 path를 만드는데 도움을 주는 GUI
//
//---------------------------------------------------------------------------------------------------------------------------------------------------------------
//  2020.09.01 _ KimYC1223
//===============================================================================================================================================================
#region PathGenerator
namespace CurvedPathGenertator {

    [CustomEditor(typeof(PathGenerator))]
    class PathGeneratorGUI : Editor {
        #region PathGenerator_GUI_Variables
        static public LANGUAGE CurrentLanguage = LANGUAGE.ENG;

        private Vector3 old_Position = new Vector3(0,0,0);                              // position value
        private Vector3 old_Scale = new Vector3(1f,1f,1f);                              // scale value
        private Quaternion old_Rotation = Quaternion.identity;                          // rotation value
        private Vector2 scrollPosition;                                                 // Node list scroll position
        private Vector2 scrollPosition2;                                                // Angle list scroll position
        private int nodeListEditIndex = -1;                                             // node index to edit
        private int angleListEditIndex = -1;                                            // angle index tod edit
        private bool isShowIndex = true;                                                // node variable to show index label
        private bool isShowEditorSetting = false;                                       // node variable to show editor setting panel

        private bool isTopViewMode = false;                                             // node variable of top view mode
        private Quaternion OldSceneViewRotation = Quaternion.identity;                  // screen view rotation value
        private float OldSceneViewSize = 0;                                             // screen view size value
        private Vector3 OldSceneViewPiviot = Vector3.zero;                              // screen view pivot value

        private GUIStyle ComponentTitle;                                                // GUI style : Component Title
        public GUIStyle H1Text;                                                         // GUI style : H1
        public GUIStyle BoldText;                                                       // GUI style : H1
        private Texture2D scrollViewBG;                                                 // Scroll view (node list, angle list) background

        private Color GuidLineColor_1 = Color.green;                                    // Guidline preview color 1
        private Color GuidLineColor_2 = Color.yellow;                                   // Guidline preview color 2
        private Color GuidLineColor_3 = Color.cyan;                                     // Path preview color

        private float SetValue = 0;                                                     // Set ( X | Y | Z ) of all nodes and angles to this value
        #endregion

        #region PathGenerator_TransformMethods
        //=======================================================================================================================================================
        //  Transform point method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  A method that changes the world coordinates according to the parent's transform information
        //  부모의 transform 정보에 따라 world 좌표를 변경하는 메소드
        //
        //  It proceeds in the order of " Rotate -> Scale up / down -> Move "
        //  " 회전 -> 스케일 업 / 다운 -> 이동 " 순서로 진행 됨
        //=======================================================================================================================================================
        private Vector3 TransformPoint(Vector3 points, Matrix4x4 m_rotate) {
            PathGenerator pathGenerator = target as PathGenerator;
            Vector3 result = points;

            result = m_rotate.MultiplyPoint3x4(result);                                 // Step1 . Rotate
            result += pathGenerator.transform.position;                                 // Step3 . Move
            result = new Vector3(                                                       // Step2 . Scale-up/down
                result.x * pathGenerator.transform.lossyScale.x,
                result.y * pathGenerator.transform.lossyScale.y,
                result.z * pathGenerator.transform.lossyScale.z
            );
            return result;
        }

        //=======================================================================================================================================================
        //  Revers transform point method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  A method that changes world coordinates to local coordinates according to the parent's transform information
        //  world 좌표를 부모의 transform 정보에 따라 local 좌표로 변경하는 메소드
        //
        //  It proceeds in the order of " Move -> Scale up / down -> Rotate "          * Reverse order of TransformPoint methods
        //  " 이동 -> 스케일 업 / 다운 -> Rotate " 순서로 진행 됨                      * TransformPoint 메소드의 역순서임
        //=======================================================================================================================================================
        private Vector3 ReverseTransformPoint(Vector3 points, Matrix4x4 m_reverse) {
            PathGenerator pathGenerator = target as PathGenerator;
            Vector3 result = points;

            result = result - pathGenerator.transform.position;                         // Step1 . Move           
            result = m_reverse.MultiplyPoint3x4(result);                                // Step3 . Rotate
            result = new Vector3(                                                       // Step2 . Scale-up/down
                result.x / pathGenerator.transform.lossyScale.x,
                result.y / pathGenerator.transform.lossyScale.y,
                result.z / pathGenerator.transform.lossyScale.z
            );
            return result;
        }
        #endregion

        #region PathGenerator_InspectorUI_Main
        //=======================================================================================================================================================
        // OnSceneGUI method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        // A function called whenever the inspector is drawn.
        // 인스펙터가 그려질 때 마다 호출되는 함수
        //=======================================================================================================================================================
        public override void OnInspectorGUI() {

            #region PathGenerator_InspectorUI_Main_StartsUp
            PathGenerator pathGenerator = target as PathGenerator;

            //===================================================================================================================================================
            //  GUI Styles
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Define and set GUI styles.
            //  GUI 스타일 정의.
            //===================================================================================================================================================
            if (ComponentTitle == null) {
                ComponentTitle = new GUIStyle(EditorStyles.label);
                ComponentTitle.normal.textColor = new Color(0f, 0.3882f, 0.9725f, 1f);
                ComponentTitle.fontSize = 17;
                ComponentTitle.fontStyle = FontStyle.Bold;
            }

            if(H1Text == null) {
                H1Text = new GUIStyle(EditorStyles.label);
                H1Text.fontStyle = FontStyle.Bold;
                H1Text.fontSize = 15;
            }

            if (BoldText == null) {
                BoldText = new GUIStyle(EditorStyles.label);
                BoldText.fontStyle = FontStyle.Bold;
            }
            #endregion

            #region PathGenerator_InspectorUI_Main_Header
            //===================================================================================================================================================
            // Title Image
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Draw head image.
            //  헤드 이미지 그리기.
            //===================================================================================================================================================
            Texture LogoTex = (Texture2D)Resources.Load("PathGenerator/Logo/PathGeneratorScriptImg", typeof(Texture2D));
            GUILayout.Label(LogoTex, GUILayout.Width(300), GUILayout.Height(67.5f));

            //===================================================================================================================================================
            //  Language setting buttons
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Supports English, Korean and Japanese. Chinese will be added later.
            //  영어, 한국어, 일본어를 지원함. 중국어는 추후 추가 예정.
            //===================================================================================================================================================
            GUILayout.BeginHorizontal();
            GUI.enabled = CurrentLanguage != LANGUAGE.ENG;
            if (GUILayout.Button("English", GUILayout.Height(22))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                CurrentLanguage = LANGUAGE.ENG;
            }
            GUI.enabled = CurrentLanguage != LANGUAGE.KOR;
            if (GUILayout.Button("한국어", GUILayout.Height(22))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                CurrentLanguage = LANGUAGE.KOR;
            }
            GUI.enabled = CurrentLanguage != LANGUAGE.JAP;
            if (GUILayout.Button("日本語", GUILayout.Height(22))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                CurrentLanguage = LANGUAGE.JAP;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            //===================================================================================================================================================
            //  Language setting buttons
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Display component information and developer information.
            //  컴포포넌트 정보와 개발자 정보를 표시,
            //===================================================================================================================================================
            GUILayout.Label( PathGeneratorGUILanguage.GetLocalText("PG_Title"), ComponentTitle);
            GUI.enabled = false;
            GUILayout.Label( PathGeneratorGUILanguage.GetLocalText("PG_SubTitle") );
            GUI.enabled = true;
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_PathDensity"));
            pathGenerator.PathDensity = EditorGUILayout.IntSlider(pathGenerator.PathDensity, 2, 60);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            //===================================================================================================================================================
            // Live path selection button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Choose live path type
            // 라이브 패스로 할지 말지 정하는 버튼
            //===================================================================================================================================================
            if (pathGenerator.isLivePath != GUILayout.Toggle(pathGenerator.isLivePath,
                                                               PathGeneratorGUILanguage.GetLocalText("PG_PathTypeChangeButton_isLivePath")) ) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                pathGenerator.isLivePath = !pathGenerator.isLivePath;
            }

            if (pathGenerator.isLivePath) {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox(PathGeneratorGUILanguage.GetLocalText("PG_PathTypeChangeButton_isLivePathWarning"), MessageType.Warning);
            }
            GUILayout.Space(5);

            //===================================================================================================================================================
            // Open / Close path selection button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Choose open / close path type
            // 열린 패스 / 닫힌 패스를 정하는 버튼
            //===================================================================================================================================================
            if (pathGenerator.isClosed) {
                if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_PathTypeChangeButton_ToOpen"), GUILayout.Height(25))) {
                    Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                    pathGenerator.isClosed = false;
                }
            } else {
                if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_PathTypeChangeButton_ToClose"), GUILayout.Height(25))) {
                    Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                    if (pathGenerator.NodeList != null && pathGenerator.NodeList.Count >= 2) {
                        Vector3 centerPos = pathGenerator.NodeList[pathGenerator.NodeList.Count - 1] + pathGenerator.NodeList[0];
                        centerPos /= 2;
                        pathGenerator.AngleList.Add(centerPos);
                    }
                    pathGenerator.isClosed = true;
                }
            }
            GUILayout.Space(15);
            GuiLine();
            GUILayout.Space(15);
            #endregion

            #region PathGenerator_InspectorUI_Main_Nodes
            //===================================================================================================================================================
            // Node list
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Panel to manage nodes
            // 노드를 관리 할 수 있는 패널
            //===================================================================================================================================================
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_H1_Node"),H1Text);
            GUILayout.Space(3);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeList_Label"));
            GUILayout.Space(5);

            //===================================================================================================================================================
            // Node list table head
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Define the head of the table
            // 표의 head 부분을 정의
            //===================================================================================================================================================
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_No"), EditorStyles.toolbarButton, GUILayout.Width(30f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_LocalPosition"), 
                                                    EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.currentViewWidth - 175f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Edit"), EditorStyles.toolbarButton, GUILayout.Width(50f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Delete"), EditorStyles.toolbarButton, GUILayout.Width(72f));
            GUILayout.EndHorizontal();

            //===================================================================================================================================================
            // Node list table
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Scroll view showing information of node list
            // Node list의 정보를 나타내는 스크롤 뷰
            //===================================================================================================================================================
            if (scrollViewBG == null) {
                scrollViewBG = new Texture2D(1, 1);
                scrollViewBG.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            }
            GUI.skin.scrollView.normal.background = scrollViewBG;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true,GUILayout.Height(100));
            try {
                //===============================================================================================================================================
                //  CASE : Empty list
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                //  if the list is empty
                //  리스트가 비어있을 경우
                //===============================================================================================================================================
                if (pathGenerator.NodeList == null || pathGenerator.NodeList.Count == 0) {
                    Rect rect0 = EditorGUILayout.GetControlRect(false, 300);
                    GUIStyle centerStyle = new GUIStyle("Label");
                    centerStyle.alignment = TextAnchor.MiddleCenter;
                    centerStyle.fixedHeight = 90;
                    GUILayout.BeginArea(rect0);
                    GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Empty"), centerStyle);
                    GUILayout.EndArea();

                //===============================================================================================================================================
                //  CASE : default
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                //  If the list can be displayed normally
                //  정상적으로 리스트를 보여 줄 수 있을 경우
                //===============================================================================================================================================
                } else {
                    Rect rect0 = EditorGUILayout.GetControlRect(false, 21 * pathGenerator.NodeList.Count);
                    Rect rect1 = new Rect(rect0.x, rect0.y, EditorGUIUtility.currentViewWidth - 36f, 21 * pathGenerator.NodeList.Count);
                    GUILayout.BeginArea(rect1);

                    //============================================================================================================================================
                    //  Loop for node list
                    //--------------------------------------------------------------------------------------------------------------------------------------------
                    //  It loops as many times as the number of elements in Node list.
                    //  Node list의 원소 수 만큼 루프를 돔
                    //============================================================================================================================================
                    for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                        Vector3 targetPos = pathGenerator.NodeList[i];
                        GUILayout.BeginHorizontal(EditorStyles.toolbar);
                        GUILayout.Label((i+1).ToString(), GUILayout.Width(30f));                            // Index
                        GUI.enabled = false;                                                                // Position value
                        EditorGUILayout.Vector3Field("", pathGenerator.NodeList[i], GUILayout.Width(EditorGUIUtility.currentViewWidth - 185f));
                        GUI.enabled = true;
                        if (GUILayout.Button(                                                               // Edit button
                            PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Edit"), 
                            EditorStyles.toolbarButton, GUILayout.Width(50f)))
                                EditNodeButtonClick(i);
                        if (GUILayout.Button(                                                               // Delete button
                            PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_DeleteButton"),
                            EditorStyles.toolbarButton, 
                            GUILayout.Width(59f))) 
                                DeleteNodeButtonClick(i);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndArea();
                }

            //===================================================================================================================================================
            //  Exception handling
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  In case of any error (e.g. there is a problem with Index)
            //  어떤 에러가 발생했을 경우 (예 : Index에 문제가 있음)
            //===================================================================================================================================================
            } catch (System.Exception e) {
                Rect rect0 = EditorGUILayout.GetControlRect(false, 300);
                GUIStyle centerStyle = new GUIStyle("Label");
                centerStyle.alignment = TextAnchor.MiddleCenter;
                centerStyle.fixedHeight = 90;
                GUILayout.BeginArea(rect0);
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Empty"), centerStyle);
                GUILayout.EndArea();
                e.ToString();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            //===================================================================================================================================================
            //  Create node button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  create node and add to list
            //  새 노드를 만들고 기존 리스트에 추가함
            //===================================================================================================================================================
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_NodeList_CreateNodeButton"), GUILayout.Height(25f)))
                CreateNodeButtonClick();
            GUILayout.Space(15);
            GuiLine();
            GUILayout.Space(15);
            #endregion

            #region PathGenerator_InspectorUI_Main_Angles
            //===================================================================================================================================================
            // Angle list
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Panel to manage angles
            // 앵글을 관리 할 수 있는 패널
            //===================================================================================================================================================
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_H1_Angle"), H1Text);
            GUILayout.Space(3);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_AngleList_Label"));
            GUILayout.Space(5);

            //===================================================================================================================================================
            // Angle list table head
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Define the head of the table
            // 표의 head 부분을 정의
            //===================================================================================================================================================
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_AngleList_From"), EditorStyles.toolbarButton, GUILayout.Width(41f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_AngleList_To"), EditorStyles.toolbarButton, GUILayout.Width(41f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_LocalPosition"),
                                                    EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.currentViewWidth - 185f));
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Edit"), EditorStyles.toolbarButton, GUILayout.Width(80f));
            GUILayout.EndHorizontal();

            //===================================================================================================================================================
            // Node list table
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Scroll view showing information of node list
            // Node list의 정보를 나타내는 스크롤 뷰
            //===================================================================================================================================================
            GUI.skin.scrollView.normal.background = scrollViewBG;
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, false, true, GUILayout.Height(100));
            try {
                //===============================================================================================================================================
                //  CASE : Empty list
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                //  if the list is empty
                //  리스트가 비어있을 경우
                //===============================================================================================================================================
                if (pathGenerator.AngleList == null || pathGenerator.AngleList.Count == 0) {
                    Rect rect0 = EditorGUILayout.GetControlRect(false, 300);
                    GUIStyle centerStyle = new GUIStyle("Label");
                    centerStyle.alignment = TextAnchor.MiddleCenter;
                    centerStyle.fixedHeight = 90;
                    GUILayout.BeginArea(rect0);
                    GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Empty"), centerStyle);
                    GUILayout.EndArea();

                //===============================================================================================================================================
                //  CASE : default
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                //  If the list can be displayed normally
                //  정상적으로 리스트를 보여 줄 수 있을 경우
                //===============================================================================================================================================
                } else {
                    Rect rect0 = EditorGUILayout.GetControlRect(false, 21 * pathGenerator.AngleList.Count);
                    Rect rect1 = new Rect(rect0.x, rect0.y, EditorGUIUtility.currentViewWidth - 36f, 21 * pathGenerator.AngleList.Count);
                    GUILayout.BeginArea(rect1);

                    //============================================================================================================================================
                    //  Loop for node list
                    //--------------------------------------------------------------------------------------------------------------------------------------------
                    //  It loops as many times as the number of elements in Angle list.
                    //  Angle list의 원소 수 만큼 루프를 돔
                    //============================================================================================================================================
                    for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                        Vector3 targetPos = pathGenerator.AngleList[i];
                        GUILayout.BeginHorizontal(EditorStyles.toolbar);
                        GUILayout.Label(( i + 1 ).ToString(), GUILayout.Width(39f));                        // Indexs
                        GUILayout.Label( ( (pathGenerator.isClosed && i == pathGenerator.AngleList.Count - 1) ? 0 : (i + 2) ).ToString(), GUILayout.Width(39f));
                        GUI.enabled = false;                                                                // Position value
                        EditorGUILayout.Vector3Field("", pathGenerator.AngleList[i], GUILayout.Width(EditorGUIUtility.currentViewWidth - 195f));
                        GUI.enabled = true;
                        if (GUILayout.Button(                                                               // Edit button
                            PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Edit"),
                            EditorStyles.toolbarButton, GUILayout.Width(80f)))
                            EditAngleButtonClick(i);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndArea();
                }

            //===================================================================================================================================================
            //  Exception handling
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  In case of any error (e.g. there is a problem with Index)
            //  어떤 에러가 발생했을 경우 (예 : Index에 문제가 있음)
            //===================================================================================================================================================
            } catch (System.Exception e) {
                Rect rect0 = EditorGUILayout.GetControlRect(false, 300);
                GUIStyle centerStyle = new GUIStyle("Label");
                centerStyle.alignment = TextAnchor.MiddleCenter;
                centerStyle.fixedHeight = 90;
                GUILayout.BeginArea(rect0);
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_NodeListTable_Empty"), centerStyle);
                GUILayout.EndArea();
                e.ToString();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(15);
            GuiLine();

            #region PathGenerator_InspectorUI_Main_TotalControl
            //===================================================================================================================================================
            // Node list
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Panel to manage nodes
            // 노드를 관리 할 수 있는 패널
            //===================================================================================================================================================
            GUILayout.Space(15);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl"), H1Text);
            GUILayout.Space(3);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_Label"));
            GUILayout.Space(5);

            //===================================================================================================================================================
            // X | Y | Z to 0
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Set ( X | Y | Z ) of all node and angles to 0
            // 모든 노드와 앵글에 대한 ( X | Y | Z ) 값을 0으로 설정
            //===================================================================================================================================================
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetZeroToX"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                Xto0ButtonClick();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetZeroToY"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                Yto0ButtonClick();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetZeroToZ"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                Zto0ButtonClick();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            //===================================================================================================================================================
            // X | Y | Z to AVG
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Set ( X | Y | Z ) of all node and angles to average
            // 모든 노드와 앵글에 대한 ( X | Y | Z ) 값을 평균값으로으로 설정
            //===================================================================================================================================================
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetAvgToX"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                XtoAVGButtonClick();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetAvgToY"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                YtoAVGButtonClick();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetAvgToZ"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                ZtoAVGButtonClick();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            //===================================================================================================================================================
            // X | Y | Z to specific value
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Set ( X | Y | Z ) of all node and angles to specific value
            // 모든 노드와 앵글에 대한 ( X | Y | Z ) 값을 특정값으로으로 설정
            //===================================================================================================================================================
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetSpecificToX"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                XtoSomethingButtonClick(SetValue);
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetSpecificToY"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                YtoSomethingButtonClick(SetValue);
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SetSpecificToZ"),
                                                GUILayout.Width(( EditorGUIUtility.currentViewWidth - 45f ) * 0.333f)))
                ZtoSomethingButtonClick(SetValue);
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_TotalControl_SpecificValue"));
            SetValue = EditorGUILayout.FloatField(SetValue);
            GUILayout.EndHorizontal();
            GUILayout.Space(15);
            GuiLine();
            GUILayout.Space(15);
            #endregion

            #region PathGenerator_InspectorUI_Main_Rendering
            //===================================================================================================================================================
            // Rendering
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Panel to rendering path
            // 경로를 렌더링하는 패널
            //===================================================================================================================================================
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_H1_Rendering"), H1Text);
            GUILayout.Space(3);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_H1_Rendering_Label"));
            GUILayout.Space(5);

            Texture2D oldLineTexture = pathGenerator.LineTexture;
            float oldLineSpeed = pathGenerator.LineSpeed;
            float oldLineOpacity = pathGenerator.LineOpacity;
            float oldLineTiling = pathGenerator.LineTiling;
            int oldLineRenderQueue = pathGenerator.LineRenderQueue;
            bool oldCreateMeshFlag = pathGenerator.CreateMeshFlag;

            pathGenerator.CreateMeshFlag = 
                GUILayout.Toggle(pathGenerator.CreateMeshFlag,PathGeneratorGUILanguage.GetLocalText("PG_Rendering_isGeneratePathMesh"));
            GUILayout.Space(10);

            if (oldCreateMeshFlag && !pathGenerator.CreateMeshFlag)
                DeletePathMesh();

            if(pathGenerator.CreateMeshFlag) {
                GUILayout.BeginHorizontal();
                pathGenerator.LineTexture = TextureField(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_LineTexture"), pathGenerator.LineTexture);
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_MaterialInfo"), BoldText);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_LineMeshWidth"), GUILayout.Width(120f));
                pathGenerator.LineMehsWidth = EditorGUILayout.Slider(pathGenerator.LineMehsWidth, 0.1f, 5f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_ScrollSpeed"), GUILayout.Width(120f));
                pathGenerator.LineSpeed = EditorGUILayout.Slider(pathGenerator.LineSpeed, -100, 100);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_Opacity"), GUILayout.Width(120f));
                pathGenerator.LineOpacity = EditorGUILayout.Slider(pathGenerator.LineOpacity, 0, 1f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_Tilling"), GUILayout.Width(120f));
                pathGenerator.LineTiling = EditorGUILayout.Slider(pathGenerator.LineTiling, 0.1f, 100f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_RenderQueue"), GUILayout.Width(120f));
                pathGenerator.LineRenderQueue = EditorGUILayout.IntSlider(pathGenerator.LineRenderQueue, 0, 5000);
                GUILayout.EndHorizontal();

                if (pathGenerator.LineRenderQueue != 3000)
                    EditorGUILayout.HelpBox(PathGeneratorGUILanguage.GetLocalText("PG_Rendering_RenderQueueHelp"), MessageType.Info);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (oldLineTexture != pathGenerator.LineTexture || oldLineSpeed != pathGenerator.LineSpeed ||
                    oldLineOpacity != pathGenerator.LineOpacity || oldLineTiling != pathGenerator.LineTiling ||
                    oldLineRenderQueue != pathGenerator.LineRenderQueue) {
                    SetMaterail();
                }
            }

            GUILayout.Space(15);

            #endregion

            #endregion
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions
        #region PathGenerator_InspectorUI_Functions_GuiLine
        //=======================================================================================================================================================
        //  GUI Line method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  draw a line on GUI Layout
        //  GUI Layout에 선을 그림
        //=======================================================================================================================================================
        private void GuiLine(int i_height = 1) {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_CreateNodeButton
        //=======================================================================================================================================================
        //  Create Node Button Click method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Method that runs when you click Create Node Button
        //  Create Node Button을 클릭했을 때 실행되는 메소드
        //=======================================================================================================================================================
        private void CreateNodeButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);

            // Exception handling
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            //===================================================================================================================================================
            //  Calculate new position
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Calculates the coordinates of the new node to be added
            //  추가할 새 노드의 좌표를 계산함
            //===================================================================================================================================================
            if (pathGenerator.isClosed) {
                // For closed paths, create at the midpoint of the first and last nodes
                Vector3 new_pos = pathGenerator.NodeList[pathGenerator.NodeList.Count - 1] + pathGenerator.NodeList[0];
                new_pos /= 2;
                pathGenerator.NodeList.Add(new_pos);

                // Modify the existing angle based on the new
                new_pos = pathGenerator.NodeList[pathGenerator.NodeList.Count - 2] + 
                                                                pathGenerator.NodeList[pathGenerator.NodeList.Count - 1];
                new_pos /= 2;
                pathGenerator.AngleList[pathGenerator.AngleList.Count - 1] = new_pos;

                // Add new angles based on new nodes
                new_pos = pathGenerator.NodeList[pathGenerator.NodeList.Count - 1] + pathGenerator.NodeList[0];
                new_pos /= 2;
                pathGenerator.AngleList.Add(new_pos);

            } else {
                //  For open paths, add to the extension of the last angle and the last node
                //  Vector Caculation : V_{lastNode} - V_{extensionAmount}
                //                    => V_{lastNode} - (V_{lastAngle} - V_{lastNode})
                //                    => 2 * V_{lastNode} - V_{lastAngle}
                Vector3 new_pos = 2 * pathGenerator.NodeList[pathGenerator.NodeList.Count - 1] -
                                     pathGenerator.AngleList[pathGenerator.AngleList.Count - 1];
                pathGenerator.NodeList.Add(new_pos);

                // Add new angles based on new nodes
                new_pos = pathGenerator.NodeList[pathGenerator.NodeList.Count - 2] +
                                                                pathGenerator.NodeList[pathGenerator.NodeList.Count - 1];
                new_pos /= 2;
                pathGenerator.AngleList.Add(new_pos);
            }

            //===================================================================================================================================================
            //  World Coordinate Transformation: Node
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  Converting the coordinates of the newly added Node to the world coordinates
            //  새로 추가된 플래그의 좌표를 월드 좌표로 변환
            //===================================================================================================================================================
            Quaternion rotation = pathGenerator.transform.rotation;                                             // Calculated based on the current Rotation value
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);                                                    // Rotation calculation Matrix
            int i = pathGenerator.NodeList.Count - 1;                                                           // Index to convert

            pathGenerator.NodeList_World.Add(pathGenerator.NodeList[i]);
            pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList_World[i],m_rotate);

            //===================================================================================================================================================
            //  World Coordinate Transformation: Angle
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            //  For closed paths, convert coordinates of newly added angle to world coordinates
            //  닫힌 경로일 경우, 새로 추가된 앵글의 좌표를 월드 좌표로 변환
            //===================================================================================================================================================
            if (pathGenerator.isClosed) {
                pathGenerator.AngleList_World.Add(pathGenerator.AngleList[i]);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList_World[i], m_rotate);
            }

            EditNodeButtonClick(pathGenerator.NodeList.Count - 1);          // For created nodes, open the Edit window immediately                                     
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_DeleteNodeButton
        //=======================================================================================================================================================
        //  Delete Node Button Click method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Method that runs when you click Delete Node Button
        //  Delete Node Button을 클릭했을 때 실행되는 메소드
        //=======================================================================================================================================================
        private void DeleteNodeButtonClick(int i) {
            PathGenerator pathGenerator = target as PathGenerator;
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);

            // Cannot be deleted if the number of nodes is less than 2 or the number of angles is less than 1.
            if (pathGenerator.NodeList == null || pathGenerator.NodeList.Count <= 2 ||
               pathGenerator.AngleList == null || pathGenerator.AngleList.Count <= 1)
                return;

            // Remove the node of the wanted index
            pathGenerator.NodeList.RemoveAt(i);
            pathGenerator.NodeList_World.RemoveAt(i);

            //If the path is closed, remove the angle as well.
            if (pathGenerator.isClosed || i < pathGenerator.NodeList.Count) {
                pathGenerator.AngleList.RemoveAt(i);
                pathGenerator.AngleList_World.RemoveAt(i);
            }

            // Close the edit window.
            nodeListEditIndex = -1;
            angleListEditIndex = -1;
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_EditNodeButton
        //=======================================================================================================================================================
        //  Draw texture field method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Fields that can receive texture input in the Inspector window
        //  Inpector 창에서 texture 입력을 받을 수 있는 필드
        //=======================================================================================================================================================
        private static Texture2D TextureField(string name, Texture2D texture) {
            GUILayout.BeginVertical();
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = 70;
            GUILayout.Label(name, style);
            var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
            GUILayout.EndVertical();
            return result;
        }

        //=======================================================================================================================================================
        //  Edit Node Button Click method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Method that runs when you click Edit Node Button
        //  Edit Node Button을 클릭했을 때 실행되는 메소드
        //=======================================================================================================================================================
        private void EditNodeButtonClick(int i) {
            PathGenerator pathGenerator = target as PathGenerator;
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            // If it is already pressed, close the window
            if (nodeListEditIndex == i) {
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
                return;
            }

            pathGenerator.EditMode = 1;                                     // Change state to individaul mode
            var sceneView = SceneView.lastActiveSceneView;                  
            sceneView.pivot = pathGenerator.NodeList_World[i];              // Move scene pivot point to target
            sceneView.size = 2;                                             // Set zoom amount of scene view
            OldSceneViewPiviot = sceneView.pivot;                           // Overwrite OldSceneViewPivot value
            OldSceneViewSize = sceneView.size;                              // Overwrite OldSceneViewSize value

            nodeListEditIndex = i;                                          // Open window
            angleListEditIndex = -1;                                        // Close window
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_TopViewButton
        //=======================================================================================================================================================
        //  Top View Button Click method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Method for changing the Scene view
        //  Scene view를 변경하는 메소드
        //=======================================================================================================================================================
        private void TopViewButtonClick(bool value) {
            PathGenerator pathGenerator = target as PathGenerator;
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            var sceneView = SceneView.lastActiveSceneView;
            isTopViewMode = value;

            if(isTopViewMode) {
                OldSceneViewRotation = sceneView.rotation;
                OldSceneViewSize = sceneView.size;
                OldSceneViewPiviot = sceneView.pivot;
                sceneView.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
                if ( pathGenerator.NodeList_World != null &&
                     nodeListEditIndex < pathGenerator.NodeList_World.Count &&
                     nodeListEditIndex != -1)
                    sceneView.pivot = pathGenerator.NodeList_World[nodeListEditIndex];
                else if( pathGenerator.AngleList_World != null &&
                         angleListEditIndex < pathGenerator.AngleList_World.Count &&
                         angleListEditIndex != -1)
                    sceneView.pivot = pathGenerator.AngleList_World[angleListEditIndex];
                else
                    sceneView.pivot = pathGenerator.transform.position;

                sceneView.size = 10;
                sceneView.isRotationLocked = true;
                sceneView.orthographic = true;
            } else {
                sceneView.rotation = OldSceneViewRotation;
                sceneView.size = OldSceneViewSize;
                sceneView.pivot = OldSceneViewPiviot;
                sceneView.isRotationLocked = false;
                sceneView.orthographic = false;
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_EditAngleButton
        //=======================================================================================================================================================
        //  Edit Angle Button Click method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Method that runs when you click Edit Angle Button
        //  Edit Angle Button을 클릭했을 때 실행되는 메소드
        //=======================================================================================================================================================
        private void EditAngleButtonClick(int i) {
            PathGenerator pathGenerator = target as PathGenerator;
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            // If it is already pressed, close the window
            if (angleListEditIndex == i) {
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
                return;
            }
            
            pathGenerator.EditMode = 1;                                     // Change state to individaul mode
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.pivot = pathGenerator.AngleList_World[i];             // Move scene pivot point to target
            sceneView.size = 2;                                             // Set zoom amount of scene view
            OldSceneViewPiviot = sceneView.pivot;                           // Overwrite OldSceneViewPivot value
            OldSceneViewSize = sceneView.size;                              // Overwrite OldSceneViewSize value

            angleListEditIndex = i;                                         // Open window
            nodeListEditIndex = -1;                                         // Close window
        }
        #endregion

        //=======================================================================================================================================================
        //  Calculate rotate 
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the x value of all nodes and angles to 0
        //  모든 노드와 앵글의 x 값을 0으로 설정
        //=======================================================================================================================================================

        #region PathGenerator_InspectorUI_Functions_SetXto0
        //=======================================================================================================================================================
        //  Set X value to 0 method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the x value of all nodes and angles to 0
        //  모든 노드와 앵글의 x 값을 0으로 설정
        //=======================================================================================================================================================
        private void Xto0ButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(0, pathGenerator.NodeList[i].y, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(0, pathGenerator.AngleList[i].y, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetYto0
        //=======================================================================================================================================================
        //  Set Y value to 0 method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the y value of all nodes and angles to 0
        //  모든 노드와 앵글의 y 값을 0으로 설정
        //=======================================================================================================================================================
        private void Yto0ButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, 0, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, 0, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetZto0
        //=======================================================================================================================================================
        //  Set Z value to 0 method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the z value of all nodes and angles to 0
        //  모든 노드와 앵글의 z 값을 0으로 설정
        //=======================================================================================================================================================
        private void Zto0ButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, pathGenerator.NodeList[i].y, 0);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, pathGenerator.AngleList[i].y, 0);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetXtoAVG
        //=======================================================================================================================================================
        //  Set X value to average method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the x value of all nodes and angles to average
        //  모든 노드와 앵글의 x 값을 평균값으로 설정
        //=======================================================================================================================================================
        private void XtoAVGButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null || (pathGenerator.NodeList.Count == 0 && pathGenerator.AngleList.Count == 0))
                return;

            //===================================================================================================================================================
            //  Calculate average of X
            //  평균값 구하기
            //===================================================================================================================================================
            float avg = 0;
            for (int i = 0; i < pathGenerator.NodeList.Count; i++)
                avg += pathGenerator.NodeList[i].x;
            for (int i = 0; i < pathGenerator.AngleList.Count; i++)
                avg += pathGenerator.AngleList[i].x;
            avg /= ( pathGenerator.NodeList.Count + pathGenerator.AngleList.Count );

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(avg, pathGenerator.NodeList[i].y, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(avg, pathGenerator.AngleList[i].y, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetYtoAVG
        //=======================================================================================================================================================
        //  Set Y value to average method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the y value of all nodes and angles to average
        //  모든 노드와 앵글의 y 값을 평균값으로 설정
        //=======================================================================================================================================================
        private void YtoAVGButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null || ( pathGenerator.NodeList.Count == 0 && pathGenerator.AngleList.Count == 0 ))
                return;

            //===================================================================================================================================================
            //  Calculate average of X
            //  평균값 구하기
            //===================================================================================================================================================
            float avg = 0;
            for (int i = 0; i < pathGenerator.NodeList.Count; i++)
                avg += pathGenerator.NodeList[i].y;
            for (int i = 0; i < pathGenerator.AngleList.Count; i++)
                avg += pathGenerator.AngleList[i].y;
            avg /= ( pathGenerator.NodeList.Count + pathGenerator.AngleList.Count );

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, avg, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, avg, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetZtoAVG
        //=======================================================================================================================================================
        //  Set Z value to average method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the z value of all nodes and angles to average
        //  모든 노드와 앵글의 z 값을 평균값으로 설정
        //=======================================================================================================================================================
        private void ZtoAVGButtonClick() {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null || ( pathGenerator.NodeList.Count == 0 && pathGenerator.AngleList.Count == 0 ))
                return;

            //===================================================================================================================================================
            //  Calculate average of X
            //  평균값 구하기
            //===================================================================================================================================================
            float avg = 0;
            for (int i = 0; i < pathGenerator.NodeList.Count; i++)
                avg += pathGenerator.NodeList[i].z;
            for (int i = 0; i < pathGenerator.AngleList.Count; i++)
                avg += pathGenerator.AngleList[i].z;
            avg /= ( pathGenerator.NodeList.Count + pathGenerator.AngleList.Count );

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, pathGenerator.NodeList[i].y, avg);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, pathGenerator.AngleList[i].y, avg);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetXtoSomething
        //=======================================================================================================================================================
        //  Set X value to specific value method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the x value of all nodes and angles to specific value
        //  모든 노드와 앵글의 x 값을 특정값으로 설정
        //=======================================================================================================================================================
        private void XtoSomethingButtonClick(float value) {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(value, pathGenerator.NodeList[i].y, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(value, pathGenerator.AngleList[i].y, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetYtoSomething
        //=======================================================================================================================================================
        //  Set Y value to specific value method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the y value of all nodes and angles to specific value
        //  모든 노드와 앵글의 y 값을 특정값으로 설정
        //=======================================================================================================================================================
        private void YtoSomethingButtonClick(float value) {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, value, pathGenerator.NodeList[i].z);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, value, pathGenerator.AngleList[i].z);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #region PathGenerator_InspectorUI_Functions_SetZtoSomething
        //=======================================================================================================================================================
        //  Set Z value to specific value method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //  Set the z value of all nodes and angles to specific value
        //  모든 노드와 앵글의 z 값을 특정값으로 설정
        //=======================================================================================================================================================
        private void ZtoSomethingButtonClick(float value) {
            PathGenerator pathGenerator = target as PathGenerator;
            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate = Matrix4x4.Rotate(rotation);
            Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            if (pathGenerator.NodeList == null || pathGenerator.AngleList == null)
                return;

            for (int i = 0; i < pathGenerator.NodeList.Count; i++) {
                pathGenerator.NodeList[i] = new Vector3(pathGenerator.NodeList[i].x, pathGenerator.NodeList[i].y, value);
                pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList[i], m_rotate);
            }
            for (int i = 0; i < pathGenerator.AngleList.Count; i++) {
                pathGenerator.AngleList[i] = new Vector3(pathGenerator.AngleList[i].x, pathGenerator.AngleList[i].y, value);
                pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList[i], m_rotate);
            }
        }
        #endregion

        #endregion

        #region PathGenerator_OnSceneUI_Main
        //=======================================================================================================================================================
        // OnSceneGUI method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        // A function called whenever the GUI is drawn on the screen.
        // 스크린에 GUI가 그려질 때 마다 호출되는 함수
        //=======================================================================================================================================================
        public void OnSceneGUI() {
            PathGenerator pathGenerator = target as PathGenerator;      // 조절할 PathGenerator
            HandleUtility.AddDefaultControl(isShowEditorSetting ? GUIUtility.GetControlID(FocusType.Passive) : -1);
            try {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
            } catch(System.Exception e) { e.ToString(); return; }

            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_rotate  = Matrix4x4.Rotate(rotation);
            Matrix4x4 m_reverse = Matrix4x4.Rotate(Quaternion.Inverse(rotation));
            List<Vector3> NodeList = pathGenerator.NodeList;
            List<Vector3> AngleList = pathGenerator.AngleList;
            int Density = pathGenerator.PathDensity;

            int Count = NodeList.Count;

            if (old_Position != pathGenerator.transform.localPosition ||
                old_Rotation != pathGenerator.transform.localRotation ||
                old_Scale    != pathGenerator.transform.localScale ||
                pathGenerator.NodeList_World.Count != NodeList.Count ||
                pathGenerator.AngleList_World.Count != AngleList.Count) {

                old_Position = pathGenerator.transform.localPosition;
                old_Rotation = pathGenerator.transform.localRotation;
                old_Scale = pathGenerator.transform.localScale;

                pathGenerator.NodeList_World = new List<Vector3>();
                for (int i = 0; i < NodeList.Count; i++) {
                    pathGenerator.NodeList_World.Add(NodeList[i]);
                    pathGenerator.NodeList_World[i] = TransformPoint(pathGenerator.NodeList_World[i], m_rotate);
                }

                pathGenerator.AngleList_World = new List<Vector3>();
                for (int i = 0; i < AngleList.Count; i++) {
                    pathGenerator.AngleList_World.Add(AngleList[i]);
                    pathGenerator.AngleList_World[i] = TransformPoint(pathGenerator.AngleList_World[i], m_rotate);
                }
            }

            //===================================================================================================================================================
            // Exception handling when NodeList is less than AngleList
            // NodeList가 AngleList보다 작을때의 예외 처리
            //===================================================================================================================================================
            if (NodeList.Count < AngleList.Count) {
                int offset = AngleList.Count - NodeList.Count;
                for (int i = 0; i < offset; i++) {
                    AngleList.RemoveAt(AngleList.Count - 1);
                }
            }

            //===================================================================================================================================================
            // Exception handling when there is only one node
            // Node가 하나 밖에 없거나 Anglelist가 비어있을때의 예외 처리
            //===================================================================================================================================================
            if (NodeList.Count < 2) {
                NodeList.Clear();
                NodeList.Add(new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f)));
                NodeList.Add(new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f)));
                Count = 2;
            }

            //===================================================================================================================================================
            // Exception handling when AngleList size is less than NodeList size
            // AngleList 크기가 NodeList보다 작은 경우 예외 처리
            //===================================================================================================================================================
            if (pathGenerator.isClosed && AngleList.Count < NodeList.Count) {
                AngleList.Clear();
                for(int i = 0; i < Count; i++)
                    AngleList.Add(new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f)));
            } else if (!pathGenerator.isClosed && AngleList.Count < NodeList.Count-1) {
                AngleList.Clear();
                for (int i = 0; i < Count - 1; i++)
                    AngleList.Add(new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f)));
            }

            //===================================================================================================================================================
            // Check objects of List. If it's not in the scene, create at the midpoint between the nodes.
            // List 기반으로 오브젝트를 검사. 만약, Scene에 없으면 두 Node 사이의 중점에 생성
            //===================================================================================================================================================
            for (int i = 0; i < Count; i++) {
                if ((i == Count - 1) && (!pathGenerator.isClosed))
                    continue;
                if (AngleList[i] == null) {
                    Vector3 nextNode = (i == Count - 1) ? NodeList[0] : NodeList[i + 1];
                    AngleList[i]
                        = (NodeList[i] + nextNode) / 2;
                }
            }

            //===================================================================================================================================================
            // If it is a closed loop, connect the last and first points
            // 닫힌 루프라면, 마지막과 첫 점을 이어줌
            //===================================================================================================================================================
            try {
                if (!pathGenerator.isClosed && AngleList[Count - 1] != null) {
                    AngleList.RemoveAt(AngleList.Count - 1);
                }
            } catch (System.Exception e) {
                e.ToString();
            }


            if ( pathGenerator.EditMode == 2 && ( pathGenerator.NodeList_World != null || pathGenerator.AngleList_World != null ) &&
                 ( pathGenerator.NodeList_World.Count > 0 || pathGenerator.AngleList_World.Count > 0) ) {
                Vector3 centerPos = Vector3.zero;
                for (int i = 0; i < pathGenerator.NodeList_World.Count; i++)
                    centerPos += pathGenerator.NodeList_World[i];
                for (int i = 0; i < pathGenerator.AngleList_World.Count; i++)
                    centerPos += pathGenerator.AngleList_World[i];

                
                centerPos /= ((int)pathGenerator.NodeList_World.Count + (int)pathGenerator.AngleList_World.Count);

                Vector3 new_centerPos = Handles.PositionHandle(centerPos, pathGenerator.transform.localRotation);
                Vector3 offset = new_centerPos - centerPos;
                for (int i = 0; i < pathGenerator.NodeList_World.Count; i++) {
                    pathGenerator.NodeList_World[i] += offset;
                    pathGenerator.NodeList[i] = ReverseTransformPoint(pathGenerator.NodeList_World[i],m_reverse);
                }
                for (int i = 0; i < pathGenerator.AngleList_World.Count; i++) {
                    pathGenerator.AngleList_World[i] += offset;
                    pathGenerator.AngleList[i] = ReverseTransformPoint(pathGenerator.AngleList_World[i], m_reverse);

                }
            }

            //===================================================================================================================================================
            // Calculate Bézier curve
            // 베지어 커브 계산
            //===================================================================================================================================================
            try {
                List<Vector3> pathList = new List<Vector3>();
                pathList.Add(pathGenerator.NodeList_World[0]);
                for (int i = 0; i < Count; i++) {
                    if (pathGenerator.EditMode == 1 && 
                        ((nodeListEditIndex == -1 && angleListEditIndex == -1) || nodeListEditIndex == i)) {
                        pathGenerator.NodeList_World[i] = Handles.PositionHandle(pathGenerator.NodeList_World[i],
                                                              pathGenerator.transform.localRotation);
                        pathGenerator.NodeList[i] = ReverseTransformPoint(pathGenerator.NodeList_World[i], m_reverse);
                    }

                    
                    try {
                        if ( (!pathGenerator.isClosed && ( i < Count-1) && AngleList[i] != null) || 
                                (pathGenerator.isClosed && AngleList[i] != null )) {
                            if (pathGenerator.EditMode == 1 &&
                                 ( ( nodeListEditIndex == -1 && angleListEditIndex == -1 ) || angleListEditIndex == i )) {
                                pathGenerator.AngleList_World[i] = Handles.PositionHandle(pathGenerator.AngleList_World[i],
                                                                       pathGenerator.transform.localRotation);
                                pathGenerator.AngleList[i] = ReverseTransformPoint(pathGenerator.AngleList_World[i], m_reverse);
                            }

                            Vector3 startPoint = pathGenerator.NodeList_World[i];
                            Vector3 middlePoint = pathGenerator.AngleList_World[i];
                            Vector3 endPoint = (i == Count - 1) ? pathGenerator.NodeList_World[0] : pathGenerator.NodeList_World[i + 1];

                            Handles.color = GuidLineColor_1;
                            Handles.DrawDottedLine(startPoint, middlePoint,2f);
                            Handles.color = GuidLineColor_2;
                            Handles.DrawDottedLine(middlePoint, endPoint,2f);

                            for (int j = 1; j <= Density; j++) {
                                float t = (float)j / Density;
                                Vector3 curve = (1f - t) * (1f - t) * startPoint +
                                               2 * (1f - t) * t * middlePoint +
                                               t * t * endPoint;
                                pathList.Add(curve);

                            }
                        }
                    } catch (System.Exception e) {
                        e.ToString();
                    }
                }

                Handles.color = GuidLineColor_3;
                Handles.DrawPolyLine(pathList.ToArray<Vector3>());
                if (pathGenerator.CreateMeshFlag)
                    CreateMesh(pathList);
            } catch (System.Exception e) {
                e.ToString();
            }


            if(isShowIndex) {
                if (pathGenerator.NodeList_World != null && pathGenerator.NodeList_World.Count > 0) {
                    for (int i = 0; i < pathGenerator.NodeList_World.Count; i++) {
                        if (nodeListEditIndex == i) continue;
                        DrawTextLabelOnScene(pathGenerator.NodeList_World[i], Color.green, "[ " + (i+1) + " ]", true, i);
                    }
                }

                if (pathGenerator.AngleList_World != null && pathGenerator.AngleList_World.Count > 0) {
                    for (int i = 0; i < pathGenerator.AngleList_World.Count; i++) {
                        if (angleListEditIndex == i) continue;
                        DrawTextLabelOnScene(pathGenerator.AngleList_World[i], Color.yellow,
                            ( pathGenerator.isClosed && i == pathGenerator.AngleList_World.Count - 1 ) ? 
                                                            ( (i + 1) + " → 1") : (( i + 1 ) + " → " + ( i + 2 ) ), false, i);
                    }
                }
            }

            if (nodeListEditIndex != -1) {
                angleListEditIndex = -1;
                if (pathGenerator.NodeList_World != null && pathGenerator.NodeList_World.Count > 0 &&
                    nodeListEditIndex < pathGenerator.NodeList_World.Count )
                    pathGenerator.NodeList_World[nodeListEditIndex]=
                        DrawNodeFieldOnScene(pathGenerator.NodeList_World[nodeListEditIndex],
                                                PathGeneratorGUILanguage.GetLocalText("PG_Node") + " [" + (nodeListEditIndex+1) + "]",
                                                Color.green);
            }

            if (angleListEditIndex != -1) {
                nodeListEditIndex = -1;
                if (pathGenerator.AngleList_World != null && pathGenerator.AngleList_World.Count > 0 &&
                    angleListEditIndex < pathGenerator.AngleList_World.Count)
                    pathGenerator.AngleList_World[angleListEditIndex] =
                        DrawAngleFieldOnScene(pathGenerator.AngleList_World[angleListEditIndex],
                                                PathGeneratorGUILanguage.GetLocalText("PG_Angle") + 
                                                (  pathGenerator.isClosed && (angleListEditIndex == pathGenerator.AngleList_World.Count - 1 ) ?
                                                   " [ " + ( angleListEditIndex + 1 ) + " → 1 ] "  :
                                                   " [ " + ( angleListEditIndex + 1 ) + " → " + ( angleListEditIndex + 2 ) +" ] "),
                                                Color.yellow);
            }

            if (isShowEditorSetting)
                DrawEditorSettingPanel(new Rect(10, 10, 350, 220));
            else
                DrawDefaultSettingPanel(new Rect(0, 0, 320, 160));
        }
        #endregion

        #region PathGenerator_OnSceneUI_Main_Functions
        private void DrawDefaultSettingPanel(Rect panelTransformInfo) {
            PathGenerator pathGenerator = target as PathGenerator;      // 조절할 PathGenerator

            Handles.BeginGUI();
            Vector2 paddingAmount = new Vector2(20, 15);
            Rect paddingRect = new Rect(panelTransformInfo.x + paddingAmount.x, panelTransformInfo.y + paddingAmount.y,
                                        panelTransformInfo.width - paddingAmount.x * 2, panelTransformInfo.height - paddingAmount.y * 2);
            GUILayout.BeginArea(paddingRect);

            Texture LogoTex = (Texture2D)Resources.Load("PathGenerator/Logo/PathGeneratorScriptImg", typeof(Texture2D));
            GUILayout.Label(LogoTex, GUILayout.Width(300), GUILayout.Height(67.5f));
            //===================================================================================================================================================
            // Edit mode selection button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Choose edit mode type
            // 에디터 모드를 결정하는 버튼
            //===================================================================================================================================================
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_H1_EditorSetting"), GUILayout.Height(25))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                isShowEditorSetting = true;
            }
            GUILayout.EndArea();
            Handles.EndGUI();

        }

        private void DrawEditorSettingPanel(Rect panelTransformInfo) {
            PathGenerator pathGenerator = target as PathGenerator;      // 조절할 PathGenerator

            Handles.BeginGUI(); 
            Color oldColor = GUI.color;
            GUI.color = new Color(194, 194, 194, 0.8f);
            GUI.Box(panelTransformInfo, "");
            GUI.color = oldColor;
            Handles.EndGUI();

            Vector2 paddingAmount = new Vector2(20, 15);
            Rect paddingRect = new Rect(panelTransformInfo.x + paddingAmount.x, panelTransformInfo.y + paddingAmount.y,
                                        panelTransformInfo.width - paddingAmount.x * 2, panelTransformInfo.height - paddingAmount.y * 2);
            Rect paddingRect2 = new Rect(panelTransformInfo.x + panelTransformInfo.width - paddingAmount.x - 20,
                                         panelTransformInfo.y + paddingAmount.y, 20,20);

            Handles.BeginGUI();

            GUILayout.BeginArea(paddingRect2);
            if(GUILayout.Button("X",GUILayout.Width(20),GUILayout.Height(20)))
                isShowEditorSetting = false;
            GUILayout.EndArea();


            GUILayout.BeginArea(paddingRect);
            //===================================================================================================================================================
            // Edit mode selection button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Choose edit mode type
            // 에디터 모드를 결정하는 버튼
            //===================================================================================================================================================
            if (H1Text == null) {
                H1Text = new GUIStyle(EditorStyles.label);
                H1Text.fontStyle = FontStyle.Bold;
                H1Text.fontSize = 15;
            }
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_H1_EditorSetting"), H1Text);
            GUILayout.Space(3);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_EditorModeSelect_Label"));
            GUILayout.BeginHorizontal();
            GUI.enabled = pathGenerator.EditMode != 0;
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_EditorModeSelect_Disable"), GUILayout.Height(25))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                pathGenerator.EditMode = 0;
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
            }
            GUI.enabled = pathGenerator.EditMode != 1;
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_EditorModeSelect_Individual"), GUILayout.Height(25))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                pathGenerator.EditMode = 1;
            }
            GUI.enabled = pathGenerator.EditMode != 2;
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_EditorModeSelect_Total"), GUILayout.Height(25))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                pathGenerator.EditMode = 2;
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_ShowLabel_Label"));
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            isShowIndex = GUILayout.Toggle(isShowIndex, PathGeneratorGUILanguage.GetLocalText("PG_ShowLabelToggle"));
            pathGenerator.isShowingIcons = GUILayout.Toggle(pathGenerator.isShowingIcons,
                                                               PathGeneratorGUILanguage.GetLocalText("PG_ShowIconsToggle"));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            //===================================================================================================================================================
            // Open / Close path selection button
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // Choose open / close path type
            // 열린 패스 / 닫힌 패스를 정하는 버튼
            //===================================================================================================================================================
            if (GUILayout.Button(
                    ( isTopViewMode ) ? PathGeneratorGUILanguage.GetLocalText("PG_TopViewModeButton_Reset") :
                                      PathGeneratorGUILanguage.GetLocalText("PG_TopViewModeButton_toTop"),
                    GUILayout.Height(25))) {
                Undo.RecordObject(pathGenerator, "Modify " + pathGenerator.gameObject.name);
                TopViewButtonClick(!isTopViewMode);
            }
            GUILayout.Space(7);

            GUILayout.BeginHorizontal();
            GUILayout.Label(PathGeneratorGUILanguage.GetLocalText("PG_Colors_Label"));
            GuidLineColor_1 = EditorGUILayout.ColorField(GuidLineColor_1);
            GuidLineColor_2 = EditorGUILayout.ColorField(GuidLineColor_2);
            GuidLineColor_3 = EditorGUILayout.ColorField(GuidLineColor_3);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();

        }

        private void DrawTextLabelOnScene(Vector3 worldPos, Color TextColor, string Text, bool isNode, int i) {
            Vector3 guiLoc = HandleUtility.WorldToGUIPointWithDepth(worldPos);
            if (guiLoc.z < 0) return;
            var rect = new Rect(guiLoc.x - 30f, guiLoc.y + 10, 60, 20);
            Handles.BeginGUI();
            Color oldBgColor = GUI.backgroundColor;
            Color oldTextColor_normal = GUI.skin.box.normal.textColor;
            Color oldTextColor_hover = GUI.skin.box.hover.textColor;
            Color oldTextColor_active = GUI.skin.box.active.textColor;
            Color oldTextColor_focused = GUI.skin.box.focused.textColor;
            GUI.skin.box.normal.textColor = TextColor;
            GUI.skin.box.hover.textColor = TextColor;
            GUI.skin.box.active.textColor = TextColor;
            GUI.skin.box.focused.textColor = TextColor;
            GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, Text);
            if (GUI.Button(rect, "", GUIStyle.none)) {
                if (isNode) EditNodeButtonClick(i);
                else EditAngleButtonClick(i);
            }
            GUI.backgroundColor = oldBgColor;
            GUI.skin.box.normal.textColor = oldTextColor_normal;
            GUI.skin.box.hover.textColor = oldTextColor_hover;
            GUI.skin.box.active.textColor = oldTextColor_active;
            GUI.skin.box.focused.textColor = oldTextColor_focused;
            Handles.EndGUI();
        }

        private Vector3 DrawNodeFieldOnScene(Vector3 worldPos, string text, Color highlight) {
            Vector3 result = worldPos;
            Vector3 guiLoc = HandleUtility.WorldToGUIPointWithDepth(worldPos);
            if (guiLoc.z < 0) return worldPos;

            Rect BGRect = new Rect(guiLoc.x - 115f, guiLoc.y + 15, 230, 100);
            Rect UIRect = new Rect(BGRect.x + 10f, BGRect.y + 10, BGRect.width - 20, BGRect.height- 20);

            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            Color oldTextColor_label_normal = EditorStyles.label.normal.textColor;
            Color oldTextColor_label_focused = EditorStyles.label.focused.textColor;
            Color oldTextColor_label_active = EditorStyles.label.active.textColor;
            Color oldTextColor_label_hover = EditorStyles.label.hover.textColor;
            Handles.BeginGUI();
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(BGRect, "");
            GUI.color = oldColor;
            Handles.EndGUI();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.label);
            titleStyle.normal.textColor = highlight;
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;


            Handles.BeginGUI();
            GUILayout.BeginArea(UIRect);
            GUI.color = new Color(1f,1f,1f,0.6f);
            EditorStyles.label.normal.textColor = Color.white;
            EditorStyles.label.focused.textColor = highlight;
            EditorStyles.label.active.textColor = highlight;
            EditorStyles.label.hover.textColor = highlight;

            GUILayout.Label(text, titleStyle);
            GUILayout.Space(5);
            result = EditorGUILayout.Vector3Field("", worldPos, GUILayout.Width(UIRect.width));
            GUILayout.Space(5);
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_Close"))) {
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
            }

            GUI.color = oldColor;
            EditorStyles.label.normal.textColor = oldTextColor_label_normal;
            EditorStyles.label.focused.textColor = oldTextColor_label_focused;
            EditorStyles.label.active.textColor = oldTextColor_label_active;
            EditorStyles.label.hover.textColor = oldTextColor_label_hover;
            GUI.contentColor = oldContentColor;
            GUILayout.EndArea();
            Handles.EndGUI();

            return result;
        }

        private Vector3 DrawAngleFieldOnScene(Vector3 worldPos, string text, Color highlight) {
            PathGenerator pathGenerator = target as PathGenerator;      // 조절할 PathGenerator
            if (pathGenerator.NodeList == null || pathGenerator.NodeList.Count < 2 ||
                pathGenerator.AngleList == null || pathGenerator.AngleList.Count < 1 ||
                angleListEditIndex == -1)
                return worldPos;

            Vector3 result = worldPos;
            Vector3 guiLoc = HandleUtility.WorldToGUIPointWithDepth(worldPos);
            if (guiLoc.z < 0) return worldPos;

            Rect BGRect = new Rect(guiLoc.x - 115f, guiLoc.y + 15, 230, 135);
            Rect UIRect = new Rect(BGRect.x + 10f, BGRect.y + 10, BGRect.width - 20, BGRect.height - 20);

            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            Color oldTextColor_label_normal = EditorStyles.label.normal.textColor;
            Color oldTextColor_label_focused = EditorStyles.label.focused.textColor;
            Color oldTextColor_label_active = EditorStyles.label.active.textColor;
            Color oldTextColor_label_hover = EditorStyles.label.hover.textColor;
            Handles.BeginGUI();
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(BGRect, "");
            GUI.color = oldColor;
            Handles.EndGUI();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.label);
            titleStyle.normal.textColor = highlight;
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;


            Handles.BeginGUI();
            GUILayout.BeginArea(UIRect);
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            EditorStyles.label.normal.textColor = Color.white;
            EditorStyles.label.focused.textColor = highlight;
            EditorStyles.label.active.textColor = highlight;
            EditorStyles.label.hover.textColor = highlight;

            GUILayout.Label(text, titleStyle);
            GUILayout.Space(5);
            result = EditorGUILayout.Vector3Field("", worldPos, GUILayout.Width(UIRect.width));
            GUILayout.Space(5);
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_Center"))) {
                Vector3 prevPoint = pathGenerator.NodeList_World[angleListEditIndex];
                Vector3 nextPoint = ( angleListEditIndex + 1 == pathGenerator.NodeList_World.Count ) ?
                                    pathGenerator.NodeList_World[0] :
                                    pathGenerator.NodeList_World[angleListEditIndex + 1];
                result = ( prevPoint + nextPoint ) / 2;
                GUI.color = oldColor;
                EditorStyles.label.normal.textColor = oldTextColor_label_normal;
                EditorStyles.label.focused.textColor = oldTextColor_label_focused;
                EditorStyles.label.active.textColor = oldTextColor_label_active;
                EditorStyles.label.hover.textColor = oldTextColor_label_hover;
                GUI.contentColor = oldContentColor;
                SceneView.lastActiveSceneView.pivot = result;
                return result;
            }
            GUILayout.Space(15);
            if (GUILayout.Button(PathGeneratorGUILanguage.GetLocalText("PG_Close"))) {
                nodeListEditIndex = -1;
                angleListEditIndex = -1;
            }

            GUI.color = oldColor;
            EditorStyles.label.normal.textColor = oldTextColor_label_normal;
            EditorStyles.label.focused.textColor = oldTextColor_label_focused;
            EditorStyles.label.active.textColor = oldTextColor_label_active;
            EditorStyles.label.hover.textColor = oldTextColor_label_hover;
            GUI.contentColor = oldContentColor;
            GUILayout.EndArea();
            Handles.EndGUI();

            return result;
        }

        public void OnEnable() {
            PathGeneratorGUILanguage.InitLocalization();
            isTopViewMode = false;
            isShowEditorSetting = true;
        }

        public void OnDisable() {
            try {
                PathGenerator pathGenerator = target as PathGenerator;
                pathGenerator.ResetTools();
                if (isTopViewMode)
                    TopViewButtonClick(false);
            } catch(SystemException e) {
                e.ToString();
            }
        }

        #endregion


        #region PathGenerator_CreateMeshFunction
        //=======================================================================================================================================================
        // Draw path mesh method
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        // Calculate vertices and triangles of path mesh
        // 경로 메쉬의 정점과 삼각형 계산
        //=======================================================================================================================================================
        private void CreateMesh(List<Vector3> pathVec) {
            PathGenerator pathGenerator = target as PathGenerator;
            if (!pathGenerator.CreateMeshFlag) return;
            MeshFilter PathMesh = pathGenerator.transform.GetComponent<MeshFilter>();
            if (PathMesh == null) return;

            Quaternion rotation = pathGenerator.transform.rotation;
            Matrix4x4 m_reverse = Matrix4x4.Rotate(Quaternion.Inverse(rotation));

            int verNum = 2 * pathVec.Count;
            int triNum = 6 * ( pathVec.Count -1 );
            Vector3[] vertices = new Vector3[verNum];
            int[] triangles = new int[triNum];
            Vector2[] uvs = new Vector2[verNum];

            float MaxLength = 0, currentLength = 0;
            for (int i = 1; i < pathVec.Count; i++)
                MaxLength += ( pathVec[i] - pathVec[i - 1] ).magnitude;

            for (int i = 0; i < pathVec.Count - 1; i++) {
                Vector3 dir = ( pathVec[i + 1] - pathVec[i] ).normalized;
                Vector3 new_dir1 = new Vector3(dir.z, 0, -dir.x);
                Vector3 new_dir2 = new Vector3(-dir.z, 0, dir.x);

                //===============================================================================================================================================
                //  Calculate the first part of the path
                //  경로의 처음 부분 계산
                //===============================================================================================================================================
                if (i == 0) {
                    vertices[2 * i] = ReverseTransformPoint(pathVec[i] + ( new_dir1 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    vertices[2 * i + 1] = ReverseTransformPoint(pathVec[i] + ( new_dir2 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    uvs[2 * i] = new Vector2(0.5f, -0.5f);
                    uvs[2 * i + 1] = new Vector2(-0.5f, -0.5f);

                //===============================================================================================================================================
                //  Calculate the middle part of the path
                //  경로의 중간 부분 계산
                //===============================================================================================================================================
                } else if (i > 0) {
                    currentLength += ( pathVec[i] - pathVec[i - 1] ).magnitude;

                    vertices[2 * i] = ReverseTransformPoint(pathVec[i] + ( new_dir1 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    vertices[2 * i + 1] = ReverseTransformPoint(pathVec[i] + ( new_dir2 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    uvs[2 * i] = new Vector2(0.5f, -0.5f + ( currentLength ) / ( MaxLength ));
                    uvs[2 * i + 1] = new Vector2(-0.5f, -0.5f + ( currentLength ) / ( MaxLength ));
                }

                //===============================================================================================================================================
                //  Calculate the last part of the path
                //  경로의 마지막 부분 계산
                //===============================================================================================================================================
                if (i == pathVec.Count - 2) {
                    vertices[2 * i + 2] = ReverseTransformPoint(pathVec[i + 1] + ( new_dir1 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    vertices[2 * i + 3] = ReverseTransformPoint(pathVec[i + 1] + ( new_dir2 * ( pathGenerator.LineMehsWidth / 2 ) ), m_reverse);
                    uvs[2 * i + 2] = new Vector2(0.5f, 0.5f);
                    uvs[2 * i + 3] = new Vector2(-0.5f, 0.5f);
                }
            }

            //===================================================================================================================================================
            //  Calculate triangles in mesh
            //  메쉬의 삼각형 계산
            //===================================================================================================================================================
            for (int i = 0; i < pathVec.Count - 1; i ++) {
                triangles[6 * i] = 2 * i + 3;
                triangles[6 * i + 1] = 2 * i + 2;
                triangles[6 * i + 2] = 2 * i;
                triangles[6 * i + 3] = 2 * i + 3;
                triangles[6 * i + 4] = 2 * i;
                triangles[6 * i + 5] = 2 * i + 1;
            }

            //===================================================================================================================================================
            //  Apply after mesh creation
            //  메쉬 생성 후 적용
            //===================================================================================================================================================
            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            newMesh.uv = uvs;
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            PathMesh.mesh = newMesh;
        }

        public void SetMaterail() {
            PathGenerator pathGenerator = target as PathGenerator;
            if (!pathGenerator.CreateMeshFlag) return;
            MeshRenderer renderer = pathGenerator.transform.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            try {
                Material newMat = new Material(Shader.Find("PathGenerator/ScrollingArrow"));
                newMat.SetTexture("_MainTex", pathGenerator.LineTexture);
                newMat.SetTextureScale("_MainTex", new Vector2(1f, pathGenerator.LineTiling));
                newMat.SetFloat("_Speed", pathGenerator.LineSpeed);
                newMat.SetFloat("_Alpha", pathGenerator.LineOpacity);
                newMat.renderQueue = pathGenerator.LineRenderQueue;

                renderer.material = newMat;
            } catch (System.Exception e) {
                renderer.material = null;
                e.ToString();
            }
        }

        public void DeletePathMesh() {
            PathGenerator pathGenerator = target as PathGenerator;
            MeshFilter PathMesh = pathGenerator.transform.GetComponent<MeshFilter>();
            if (PathMesh == null) return;

            try {
                PathMesh.mesh = null;
            } catch (System.Exception e) {
                e.ToString();
            }
        }

        #endregion
    }

}
#endregion