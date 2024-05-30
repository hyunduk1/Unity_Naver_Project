using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CONTENTS_ID
{
    PC_CONTROL_CENTER = 1001,
    PC_NAMU_CONDITIONING = 1002,
    PC_LAUNG_TABLE_CONTENTS = 1003,
    PC_ROBOT = 1004,
    PC_DEFAULT = 1005
}


public enum PROTOCOL
{
    /// <summary>
    /// Server, Client BrodCasting
    /// </summary>
    /// 

    MSG_CLIENT_CAPTURE          =0,
    MSG_CLIENT_DESTROY_CAPTURE  =1,
    MSG_CLIENT_PAUSE_CAPTURE    =2,
    MSG_VIDEO_00                =3,
    MSG_VIDEO_01                =4,
    MSG_VIDEO_02                =5,
    MSG_VIDEO_03                =6,
    MSG_VIDEO_04                =7,
    MSG_VIDEO_05                =8,
    MSG_VIDEO_06                =9,
    MSG_VIDEO_07                =10,
    MSG_VIDEO_08                =11,
    MSG_VIDEO_09                =12,


    MSG_EN                      =20,
    MSG_KR                      =21,
    MSG_NUM                     =22,


    /// <summary>
    /// LANGE OPTION PAGE
    /// </summary>
    MSG_LANGUAGE_ENGLISH_CONTROL = 151,   //{“id”:151,}-> EN
    MSG_LANGUAGE_TOGGLE_EN_KR_CONTROL = 152,   //{“id”:152}-> EN

/// <summary>
/// CONTROL CENTER PAGE
/// </summary>
    MSG_CONTROLL_AUTO_START = 300,//{“id”:300}
    MSG_CONTROLL_MANUAL_START = 301,//{“id”:301}

    MSG_CONTROL_GUP_SERVER = 302,//{“id”:302}
    MSG_CONTROL_SERVER_LAG = 303,//{“id”:303}
    MSG_CONTROL_SERVER_ROOM = 304,//{“id”:304}
    MSG_CONTROL_ROBOT = 305,//{“id”:305}

    MSG_CONTROL_INFRA_STRUCTURE = 306,//{“id”:306}
    MSG_CONTROL_POWER_REDUNDENCY = 307,//{“id”:307}
    MSG_CONTROL_MULTIPLEXING = 308,//{“id”:308}
    MSG_CONTROL_IDC_DUALIZATION = 309,//{“id”:309}
    MSG_CONTROL_DOMESTIC_IDC = 310,//{“id”:310}
    MSG_CONTROL_GLOBAL_REGION = 311,//{“id”:311}

    MSG_CONTROL_EXIT = 312,//{“id”:312}
    MSG_CONTROL_PLAY_STOP = 313,//{“id”:313}
    MSG_CONTROL_NEXT_PAGE = 314,//{“id”:314}

    MSG_SET_CONTROL_STATE = 700,
    MSG_SET_NAMU_STATE = 701,
    MSG_SET_LAUNG_STATE = 702,
    MSG_SET_ROBOT_STATE = 703,

    MSG_SET_CONTENTS_ID = 1000


}

