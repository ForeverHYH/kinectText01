using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO;

namespace kinectText01
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinect; //定义一个私有变量kinect来存储对获取到的KincectSensor对象的引用
        public KinectSensor Kinect //对这个私有变量进行包装，使用属性的目的是保证能够以正确的方式初始化和反初始化KinectSensor对象
        {
           get
           { 
               return this.kinect;
           }       
            set {           
                //如果带赋值的传感器和目前的不一样           
                if (this.kinect!=value)           
                {               
                    //如果当前的传感对象不为null               
                    if (this.kinect!=null)               
                    {
                        UninitializeKinectSensor(this.kinect);
                        //uninitailize当前对象                    
                        this.kinect=null;               
                    }               
                    //如果传入的对象不为空，且状态为连接状态               
                    if (value!=null&&value.Status==KinectStatus.Connected)               
                    { 
                        this.kinect=value;
                        InitializeKinectSensor(this.kinect);
                    }           
                }       
            }    
        }
        private WriteableBitmap _ColorImageBitmap; 
        private Int32Rect _ColorImageBitmapRect; 
        private int _ColorImageStride; 
        private byte[] _ColorImagePixelData;
        private void InitializeKinectSensor(KinectSensor kinectSensor) 
        { 
            if (kinectSensor != null)
            {
                ColorImageStream colorStream = kinectSensor.ColorStream;
                kinectSensor.ColorStream.Enable();
                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorImageElement.Source = this._ColorImageBitmap;

                kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
                kinectSensor.Start();
            } 
        }
        private void UninitializeKinectSensor(KinectSensor kinectSensor) 
        {
            if (kinectSensor != null) 
            {
                kinectSensor.Stop(); 
                kinectSensor.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady); 
            }
        }
        public void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) 
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame()) 
            { 
                if (frame != null) 
                { 
                    byte[] pixelData = new byte[frame.PixelDataLength]; 
                    frame.CopyPixelDataTo(pixelData); 
                    ColorImageElement.Source = BitmapImage.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, pixelData, frame.Width * frame.BytesPerPixel); 
                } 
            }
        }
        public MainWindow()    
        {       
            InitializeComponent();       
            this.Loaded += (s, e) => DiscoverKinectSensor();        
            this.Unloaded += (s, e) => this.kinect = null;   
        }   
        private void DiscoverKinectSensor()   
        {       
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;        
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);   
        }    
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)    
        {        switch (e.Status)       
                {           
                    case KinectStatus.Connected:               
                        if (this.kinect == null)                   
                            this.kinect = e.Sensor;                
                            break;           
                    case KinectStatus.Disconnected:               
                        if (this.kinect == e.Sensor)                
                        {                   
                            this.kinect = null;                    
                            this.kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                            if (this.kinect == null)                   
                            {                       
                                //TODO:通知用于Kinect已拔出                      
                            }               
                        }               
                        break;            
                                //TODO:处理其他情况下的状态       
                 }   
        }
        private void TakePictureButton_Click(object sender, RoutedEventArgs e) 
        { 
            String fileName = "snapshot.jpg";
            if (File.Exists(fileName)) 
            { 
                File.Delete(fileName); 
            } 
            using (FileStream savedSnapshot = new FileStream(fileName, FileMode.CreateNew)) 
            { 
                BitmapSource image = (BitmapSource)ColorImageElement.Source; 
                JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder(); 
                jpgEncoder.QualityLevel = 70; jpgEncoder.Frames.Add(BitmapFrame.Create(image)); 
                jpgEncoder.Save(savedSnapshot); 
                savedSnapshot.Flush(); 
                savedSnapshot.Close();
                savedSnapshot.Dispose(); 
            } 
        }
    }
}
