using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using iDetector;

namespace IRaySDKSampleCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, EventReceiver
    {
        private int m_nId = -1;
        private IRayImage CurImage;

        private DispatcherTimer ConnectStateTimer;
        private int CorrectionOpt = 0;
        
        public MainWindow()
        {
            InitializeComponent();            
        }

        private delegate void UpdateStatus(System.Windows.Controls.ListBox listbox, string status, params object[] args);

        private void AddStatus(string status)
        {
            MsgList.Dispatcher.BeginInvoke(new UpdateStatus(UpdateListBox), MsgList, status, null);
        }

        private void AddStatus(string status, params object[] args)
        {
            MsgList.Dispatcher.BeginInvoke(new UpdateStatus(UpdateListBox), MsgList, status, args);
        }

        private void UpdateListBox(System.Windows.Controls.ListBox listbox, string txt, params object[] args)
        {
            if (args == null)
            {
                listbox.Items.Insert(0, txt);
            }
            else
            {
                listbox.Items.Insert(0, string.Format(txt, args));
            }            
        }

        private void UpdateTemperature()
        {
            AttrResult result = new AttrResult();
            Detector d = Detector.DetectorList[m_nId];
            if (d != null)
            {
                d.GetAttr(SdkInterface.Attr_RdResult_T2, ref result);
                TBTemp.Dispatcher.BeginInvoke(new Action(() =>
                {
                    TBTemp.Text = string.Format("{0:F1}", result.fVal);
                }
                ));
            }
        }

        private void UpdateTriggerMode()
        { 
            AttrResult result = new AttrResult();
            AttrResult attr = new AttrResult();
            Detector d = Detector.DetectorList[m_nId];
            string[] syncMode = {"FreeRun", "SyncIn", "SyncOut", "Unknown"};
            string[] triggerMode = {"Outer", "Inner", "Soft", "Prep", "Service", "FreeSync", "Unknown"};

            if (d != null)
            {
                d.GetAttr(SdkInterface.Attr_UROM_ProductNo, ref result);
                if ((int)Enm_ProdType.Enm_Prd_Mercu0909F == result.nVal)
                {
                    d.GetAttr(SdkInterface.Attr_UROM_FluroSync, ref attr);
                    int index = attr.nVal;
                    if (index < 0 || index > 3)   index = 3;

                    TBSyncMode.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TBSyncMode.Text = syncMode[index];
                        }
                        ));
                    
                }
                else
                {
                    d.GetAttr(SdkInterface.Attr_UROM_TriggerMode, ref attr);
                    int index = attr.nVal;

                    if (index < 0 || index > 5)  index = 5;

                    TBSyncMode.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TBSyncMode.Text = triggerMode[index];
                        }
                        ));            
                }            
            }
        }

        void EventReceiver.SdkCallbackHandler(int nDetectorID, int nEventID, int nEventLevel,
                       IntPtr pszMsg, int nParam1, int nParam2, int nPtrParamLen, IntPtr pParam)
        {
            bool processed = true;

            switch (nEventID)
            {
                case SdkInterface.Evt_TaskResult_Succeed:
                    {
                        switch (nParam1)
                        {
                            case SdkInterface.Cmd_Connect:
                               
                                AddStatus("Connect succeed!");                             
                                ConnectStateTimer.Start();                          
                                UpdateTriggerMode();
                                SetApplicationModeForMercu("Mode1");
                                break;
                            case SdkInterface.Cmd_ReadUserROM:
                                AddStatus("Read ram succeed!");
                                break;
                            case SdkInterface.Cmd_WriteUserROM:
                                AddStatus("Write ram succeed!");
                                break;
                            case SdkInterface.Cmd_Clear:
                                AddStatus("Cmd_Clear Ack succeed");
                                break;
                            case SdkInterface.Cmd_ClearAcq:
                                AddStatus("Cmd_ClearAcq Ack succeed.");
                                break;
                            case SdkInterface.Cmd_StartAcq:
                                AddStatus("Cmd_StartAcq Ack succeed.");
                                break;
                            case SdkInterface.Cmd_StopAcq:
                                AddStatus("Cmd_StopAcq Ack succeed.");
                                break;
                            case SdkInterface.Cmd_ForceSingleAcq:
                                AddStatus("Cmd_ForceSingleAcq Ack succeed.");
                                break;
                            case SdkInterface.Cmd_Disconnect:
                                AddStatus("Cmd_Disconnect Ack succeed.");
                                break;
                            case SdkInterface.Cmd_ReadTemperature:
                                AddStatus("Cmd_ReadTemperature Ack Succeed.");
                                UpdateTemperature();
                                break;
                            case SdkInterface.Cmd_SetCorrectOption:
                                AddStatus("Cmd_SetCorrectOption Ack Succeed.");
                                break;
                            case SdkInterface.Cmd_SetCaliSubset:
                                AddStatus("Cmd_SetCaliSubset Ack Succeed.");
                                break;
                            default:
                                processed = false;
                                break;
                        }
                    }
                    break;
                case SdkInterface.Evt_TaskResult_Failed:
                    switch (nParam1)
                    {
                        case SdkInterface.Cmd_Connect:
                            {
                                switch (nParam2)
                                {
                                    case SdkInterface.Err_DetectorRespTimeout:
                                        AddStatus("FPD no response!");
                                        break;
                                    case SdkInterface.Err_FPD_Busy:
                                        AddStatus("FPD busy!");
                                        break;
                                    case SdkInterface.Err_ProdInfoMismatch:
                                        AddStatus("Init failed!");
                                        break;
                                    case SdkInterface.Err_ImgChBreak:
                                        AddStatus("Image Chanel isn't ok!");
                                        break;
                                    case SdkInterface.Err_CommDeviceNotFound:
                                        AddStatus("Cannot find device!");
                                        break;
                                    case SdkInterface.Err_CommDeviceOccupied:
                                        AddStatus("Device is beeing occupied!");
                                        break;
                                    case SdkInterface.Err_CommParamNotMatch:
                                        AddStatus("Param error, please check IP address!");
                                        break;
                                    default:
                                        AddStatus("Connect failed!");
                                        break;
                                }
                            }
                            break;
                        case SdkInterface.Cmd_ReadUserROM:
                            AddStatus("Read ram failed!");
                            break;
                        case SdkInterface.Cmd_WriteUserROM:
                            AddStatus("Write ram failed!");
                            break;
                        case SdkInterface.Cmd_StartAcq:
                            AddStatus("Cmd_StartAcq Ack failed.");
                            break;
                        case SdkInterface.Cmd_StopAcq:
                            AddStatus("Cmd_StopAcq Ack failed.");
                            break;
                        case SdkInterface.Cmd_Disconnect:
                            AddStatus("Cmd_Disconnect Ack failed.");
                            break;
                        case SdkInterface.Cmd_ReadTemperature:
                            AddStatus("Cmd_ReadTemperature Ack failed.");                           
                            break;
                        case SdkInterface.Cmd_SetCorrectOption:
                            AddStatus("Cmd_SetCorrectOption Ack failed.");
                            break;
                        case SdkInterface.Cmd_ClearAcq:
                            AddStatus("Cmd_ClearAcq Ack failed.");
                            break;
                        case SdkInterface.Cmd_SetCaliSubset:
                            AddStatus("Cmd_SetCaliSubset Ack failed.");
                            break;
                        default:
                            processed = false;
                            AddStatus("Failed!");
                            break;
                    }
                    break;

                case SdkInterface.Evt_Exp_Prohibit:
                    AddStatus("Evt_Exp_Prohibit.");
                    break;
                case SdkInterface.Evt_Exp_Enable:
                    AddStatus("Evt_Exp_Enable.");
                    break;
                case SdkInterface.Evt_Image:
                case SdkInterface.Evt_Prev_Image:
                    {
                        AddStatus("Got Image");
                        CurImage = new IRayImage();
                        CurImage = (IRayImage)Marshal.PtrToStructure(pParam, typeof(IRayImage));
                        SaveImage(CurImage); // Infer or save to DB
                    }
                    break;
                default:
                    processed = false;
                    break;
            }

            if (!processed)
            {
                string msg = "unprocessed msg:" + nEventID.ToString() + ",nParam1:" + nParam1.ToString();
                AddStatus(msg);
            }
            
            return;        
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();

            folderDlg.ShowNewFolderButton = false;

            folderDlg.Description = "select work directory";
            
            if (folderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextWorkDirPath.Text = folderDlg.SelectedPath;
            }           

            folderDlg.Dispose(); 

        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            m_nId = Detector.CreateDetector(TextWorkDirPath.Text,this);
            string txt;
            if (m_nId > 0)
            {
                txt = "Create successfully.";
            }
            else
            {
                txt = "Create failed.";           
            }

            AddStatus(txt);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            string txt;
            if (d == null)
            {
                txt = "invalid detector id";
            }
            else
            {
                int nResult = d.Connect();
                txt = (nResult == 0 ? "connecting..." : "connect fail.");
            }

            AddStatus(txt);

            ConnectStateTimer = new DispatcherTimer();
            ConnectStateTimer.Interval = new TimeSpan(0, 0, 1);
            ConnectStateTimer.Tick += MonitorChannelStateChange;
        }

        private void btnDestroy_Click(object sender, RoutedEventArgs e)
        {
            Detector.DestroyDetector(m_nId);
            AddStatus("Destroy detector:{0:d}", m_nId);
            m_nId = 0;
            TBSyncMode.Text = "Unknown";
        }

        private void Canvas_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_nId > 0)
            {
                Detector.DestroyDetector(m_nId);
                m_nId = 0;
            }            
        }

        private void MonitorChannelStateChange(object sender, EventArgs e)
        {

            string[] arrSdkState = { "Unknown", "Ready", "Busy", "Sleeping" };
            string[] arrConnState = { "Unknown", "Hardbreak", "NotConnected", "LowRate", "OK" };
            IRayVariant var = new IRayVariant();
            IRayVariant connState = new IRayVariant();
            if (m_nId > 0)
            {
                int retCode = SdkInterface.GetAttr(m_nId, SdkInterface.Attr_State, ref var);
                if (SdkInterface.Err_OK != retCode) var.val.nVal = 0;

                retCode = SdkInterface.GetAttr(m_nId, SdkInterface.Attr_ConnState, ref connState);
                if (SdkInterface.Err_OK != retCode) var.val.nVal = 0;
            }

            TBSdkStatus.Text = arrSdkState[var.val.nVal];
            TBConnState.Text = arrConnState[connState.val.nVal];          
        }

        private void SaveImage(IRayImage image)
        {
            var nWidth = image.nWidth;
            var nHeight = image.nHeight;
            var nBytesPerPixel = image.nBytesPerPixel;
            var nImgSize = nWidth * nHeight * nBytesPerPixel;
            byte[] ImgData = null;

            if ((0 != nImgSize) && (IntPtr.Zero != image.pData))
            {
                ImgData = new byte[nImgSize];

                try
                {
                    Marshal.Copy(image.pData, ImgData, 0, nImgSize);
                }
                catch (Exception)
                {
                    AddStatus("Save Image failed.");
                }
            }

            if (image.propList.nItemCount > 0)
            {
                IRayVariantMapItem[] Params = new IRayVariantMapItem[image.propList.nItemCount];

                SdkParamConvertor<IRayVariantMapItem>.IntPtrToStructArray(image.propList.pItems, ref Params);
                //Params is availabe now.
            }

            SaveImageToFile(".\\curimag", ImgData);
            return;
        }
        private void SaveImageToFile(string path, byte[] data)
        {
            if (null == data || null == path)
                return;

            FileStream fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            fileStream.Write(data, 0, data.Length);
            fileStream.Close();
            AddStatus("Save Image done. .\\curimag");
            return;
        }

        private void UpdateCorrectionOption()
        {
            TBCorrectOpt.Text = string.Format("0x{0:X8}", CorrectionOpt);                  
        }

        private void SWPre_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.OFFSETMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_SW_PreOffset;
            UpdateCorrectionOption();
        }

        private void SWPost_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.OFFSETMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_SW_PostOffset;
            UpdateCorrectionOption();
        }

        private void HWPre_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.OFFSETMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_HW_PreOffset;
            UpdateCorrectionOption();
        }

        private void HWPost_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.OFFSETMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_HW_PostOffset;
            UpdateCorrectionOption();
        }

        private void OffsetNone_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.OFFSETMASK;           
            UpdateCorrectionOption();
        }

        private void SWGain_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.GAINMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_SW_Gain;
            UpdateCorrectionOption();
        }

        private void HWGain_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.GAINMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_HW_Gain;
            UpdateCorrectionOption();
        }

        private void GainNone_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.GAINMASK;           
            UpdateCorrectionOption();
        }

        private void SWDefect_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.DEFECTMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_SW_Defect;
            UpdateCorrectionOption();
        }

        private void HWDefect_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.DEFECTMASK;
            CorrectionOpt |= (int)Enm_CorrectOption.Enm_CorrectOp_HW_Defect;
            UpdateCorrectionOption();
        }

        private void DefectNone_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionOpt &= ~Detector.DEFECTMASK;
            UpdateCorrectionOption();
        }

        private void BtnSetCorrectOpt_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.SetCorrectionOption(CorrectionOpt);

            if (SdkInterface.Err_TaskPending != nRet && SdkInterface.Err_OK != nRet)
            {
                AddStatus("set correction option failed. err:%d", nRet);
            }
            else
            {
                AddStatus("setting correction...");
            }
        }

        private void BtnPrepAcq_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.PrepAcquire();

            if (SdkInterface.Err_TaskPending != nRet && SdkInterface.Err_OK != nRet)
            {
                AddStatus("PrepAcquire failed. err:%d", nRet);
            }
            else
            {
                AddStatus("PrepAcquire...");
            }

        }

        private void BtnStartAcq_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.StartAcquire();

            if (SdkInterface.Err_TaskPending != nRet && SdkInterface.Err_OK != nRet)
            {
                AddStatus("StartAcquire failed. err:%d", nRet);
            }
            else
            {
                AddStatus("StartAcquire...");
            }

        }

        private void BtnStopAcq_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.StopAcquire();

            if (SdkInterface.Err_TaskPending != nRet && SdkInterface.Err_OK != nRet)
            {
                AddStatus("StopAcquire failed. err:%d", nRet);
            }
            else
            {
                AddStatus("StopAcquire...");
            }

        }

        private void BtnReadTemp_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.ReadTemperature();

            if (SdkInterface.Err_TaskPending != nRet && SdkInterface.Err_OK != nRet)
            {
                AddStatus("ReadTemperature failed. err:%d", nRet);
            }
            else
            {
                AddStatus("ReadTemperature...");
            }
        }

        private void Test()
        {
            //open only one operation
            SetApplicationModeForMercu("Mode1");
            //SetSyncModeForMercu(Enm_FluroSync_SyncOut);
            //SetTriggerModeForStaticPrd(Enm_TriggerMode_Soft);
        }

        private void SetApplicationModeForMercu(string mode)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            AttrResult result = new AttrResult();
            d.GetAttr(SdkInterface.Attr_UROM_ProductNo, ref result);
            if ((int)Enm_ProdType.Enm_Prd_Mercu0909F == result.nVal)
            {
                int ret = d.Invoke(SdkInterface.Cmd_SetCaliSubset, mode);

                if (SdkInterface.Err_OK != ret && SdkInterface.Err_TaskPending != ret)
                {
                    AddStatus("invoke Cmd_SetCaliSubset failed. ");
                }
            }
        }

        private void SetSyncModeForMercu(Enm_FluroSync eSyncMode)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int ret = d.SetAttr(SdkInterface.Attr_UROM_FluroSync_W, (int)eSyncMode);

            if (SdkInterface.Err_OK != ret) return;

            ret = d.Invoke(SdkInterface.Cmd_WriteUserROM);
            if (SdkInterface.Err_OK != ret && SdkInterface.Err_TaskPending != ret)
            {
                AddStatus("invoke Cmd_WriteUserROM failed. FluroSync");
            }
        }

        private void SetTriggerModeForStaticPrd(Enm_TriggerMode eTriggerMode)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int ret = d.SetAttr(SdkInterface.Attr_UROM_TriggerMode_W, (int)eTriggerMode);

            if (SdkInterface.Err_OK != ret)    return;

            ret = d.Invoke(SdkInterface.Cmd_WriteUserROM);
            if (SdkInterface.Err_OK != ret && SdkInterface.Err_TaskPending != ret)
            {
                AddStatus("invoke Cmd_WriteUserROM failed. TriggerMode");
            }
        }


        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Detector d = Detector.DetectorList[m_nId];
            if (d == null) return;

            int nRet = d.Disconnect();

            if (SdkInterface.Err_OK != nRet && SdkInterface.Err_TaskPending != nRet)
            {
                AddStatus("invoke Disconnect failed.");
            }

        }

    }
}
