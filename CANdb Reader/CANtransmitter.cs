using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using vxlapi_NET;


namespace CANsharp
{
    public class CAN
    {
        // -----------------------------------------------------------------------------------------------
        // DLL Import for RX events
        // -----------------------------------------------------------------------------------------------
        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern int WaitForSingleObject(int handle, int timeOut);
        // -----------------------------------------------------------------------------------------------
        //[DllImport(@"C:\Users\Public\Documents\Vector XL Driver Library\bin\vxlapi64.dll")]

        // -----------------------------------------------------------------------------------------------
        // Global variables
        // -----------------------------------------------------------------------------------------------
        // Driver access through XLDriver (wrapper)
        private XLDriver CANDemo = new XLDriver();
        private String appName = "ViTAmin";

        // Driver configuration
        private XLClass.xl_driver_config driverConfig = new XLClass.xl_driver_config();

        // Variables required by XLDriver
        private XLDefine.XL_HardwareType hwType = XLDefine.XL_HardwareType.XL_HWTYPE_VN8950;
        private uint hwIndex = 0;
        private uint hwChannel = 0;
        private int portHandle = -1;
        private int eventHandle = -1;
        private UInt64 accessMask = 0;
        private UInt64 permissionMask = 0;
        private UInt64 txMask = 0;
        private int channelIndex = 0;

        // RX thread
        private Thread rxThread;
        private bool blockRxThread = false;
        // -----------------------------------------------------------------------------------------------
        public XLDefine.XL_Status status { get; set; }

        public CAN()
        {

        }

        // -----------------------------------------------------------------------------------------------
        /// <summary>
        /// MAIN
        /// 
        /// Sends and receives CAN messages using main methods of the "XLDriver" class.
        /// This demo requires two connected CAN channels (Vector network interface). 
        /// The configuration is read from Vector Hardware Config (vcanconf.exe).
        /// </summary>
        // -----------------------------------------------------------------------------------------------
        //[STAThread]
        public String InitCANtransmitter()
        {

            // Open XL Driver
            status = CANDemo.XL_OpenDriver();
            //Console.WriteLine("Open Driver       : " + status);
            //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();


            // Get XL Driver configuration
            status = CANDemo.XL_GetDriverConfig(ref driverConfig);
            //Console.WriteLine("Get Driver Config : " + status);
            //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

            // If the application name cannot be found in VCANCONF...
            if ((CANDemo.XL_GetApplConfig(appName, 0, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS) ||
                (CANDemo.XL_GetApplConfig(appName, 1, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS))
            {
                //...create the item with two CAN channels
                CANDemo.XL_SetApplConfig(appName, 0, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                CANDemo.XL_SetApplConfig(appName, 1, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                PrintAssignError();
            }

            else // else try to read channel assignments
            {
                string str;

                // Read setting of CAN1
                CANDemo.XL_GetApplConfig(appName, 0, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);

                // Notify user if no channel is assigned to this application 
                if (hwType == XLDefine.XL_HardwareType.XL_HWTYPE_NONE) PrintAssignError();

                accessMask = CANDemo.XL_GetChannelMask(hwType, (int)hwIndex, (int)hwChannel);
                txMask = accessMask; // this channel is used for Tx

                /*
                // Read setting of CAN2
                CANDemo.XL_GetApplConfig(appName, 1, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);

                // Notify user if no channel is assigned to this application 
                if (hwType == XLDefine.XL_HardwareType.XL_HWTYPE_NONE) PrintAssignError();

                accessMask |= CANDemo.XL_GetChannelMask(hwType, (int)hwIndex, (int)hwChannel); // OR: access both channels for RX later
                */
                permissionMask = accessMask;

                // Open port 
                status = CANDemo.XL_OpenPort(ref portHandle, appName, accessMask, ref permissionMask, 1024, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                //Console.WriteLine("\n\nOpen Port             : " + status);
                //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

                // Check port
                status = CANDemo.XL_CanRequestChipState(portHandle, accessMask);
                //Console.WriteLine("Can Request Chip State: " + status);
                //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

                // Activate channel
                status = CANDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
                //Console.WriteLine("Activate Channel      : " + status);
                //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

                // Get RX event handle
                status = CANDemo.XL_SetNotification(portHandle, ref eventHandle, 1);
                //Console.WriteLine("Set Notification      : " + status);
                //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

                // Reset time stamp clock
                CANDemo.XL_ResetClock(portHandle);
                //Console.WriteLine("Reset Clock           : " + status + "\n\n");
                //if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

            }
            return status.ToString();
        }
        // -----------------------------------------------------------------------------------------------

        public void Shutdown()
        {
            Console.WriteLine("Close Port                     : " + CANDemo.XL_ClosePort(portHandle));
            Console.WriteLine("Close Driver                   : " + CANDemo.XL_CloseDriver());
        }


        // -----------------------------------------------------------------------------------------------
        /// <summary>
        /// Error message/exit in case of a functional call does not return XL_SUCCESS
        /// </summary>
        // -----------------------------------------------------------------------------------------------
        private int PrintFunctionError()
        {
            Console.WriteLine("\nERROR: Function call failed!\nPress any key to close this application...");
            return -1;
        }
        // -----------------------------------------------------------------------------------------------

        // -----------------------------------------------------------------------------------------------
        /// <summary>
        /// Error message if channel assignment is not valid.
        /// </summary>
        // -----------------------------------------------------------------------------------------------
        private void PrintAssignError()
        {
            Console.WriteLine("\nPlease check application settings of \"" + appName + " CAN1/CAN2\" \nand assign it to an available hardware channel and restart application.");
            CANDemo.XL_PopupHwConfig();
            //Console.ReadKey();
        }
        // -----------------------------------------------------------------------------------------------


        public void CANTransmit(Message message)
        {
            CANTransmit(message.Id, message.Dlc, message.Array);
        }

        // -----------------------------------------------------------------------------------------------
        /// <summary>
        /// Sends some CAN messages.
        /// </summary>
        // ----------------------------------------------------------------------------------------------- 
        public void CANTransmit(uint id, uint dlc, byte[] array)
        {
            XLDefine.XL_Status txStatus;

            //Console.WriteLine("transmitting " + id + " : " + dlc + " ");

            // Create an event collection with 1 messages (events)
            // XLClass.xl_event_collection(2) = 2 messages
            XLClass.xl_event_collection xlEventCollection = new XLClass.xl_event_collection(1);

            // event 1
            // make copy of this and increment index of xlEvent to increase number of message
            xlEventCollection.xlEvent[0].tagData.can_Msg.id = id;
            xlEventCollection.xlEvent[0].tagData.can_Msg.dlc = (ushort)dlc;
            for (int i = 0; i < dlc; i++)
            {
            //xlEventCollection.xlEvent[0].tagData.can_Msg.data[0] = 0;
                xlEventCollection.xlEvent[0].tagData.can_Msg.data[i] = array[i];
            }
            xlEventCollection.xlEvent[0].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;

            //Console.WriteLine("start transmitting...");

            // Transmit events
            txStatus = CANDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
        }
        // -----------------------------------------------------------------------------------------------

        public void CANMultipleTransmit(List<Message> list)
        {
            XLDefine.XL_Status txStatus;

            // Create an event collection with 1 messages (events)
            // XLClass.xl_event_collection(2) = 2 messages
            XLClass.xl_event_collection xlEventCollection = new XLClass.xl_event_collection((uint)list.Count);

            int count = 0;
            foreach (Message m in list)
            {
                // event 1
                // make copy of this and increment index of xlEvent to increase number of message
                xlEventCollection.xlEvent[count].tagData.can_Msg.id = m.Id;
                xlEventCollection.xlEvent[count].tagData.can_Msg.dlc = (ushort)m.Dlc;
                for (int i = 0; i < m.Dlc; i++)
                {
                    //xlEventCollection.xlEvent[0].tagData.can_Msg.data[0] = 0;
                    xlEventCollection.xlEvent[count].tagData.can_Msg.data[i] = m.Array[i];
                }
                xlEventCollection.xlEvent[count].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;
                count++;
            }


            // Transmit events
            txStatus = CANDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
        }


        // -----------------------------------------------------------------------------------------------
        /// <summary>
        /// EVENT THREAD (RX)
        /// 
        /// RX thread waits for Vector interface events and displays filtered CAN messages.
        /// </summary>
        // ----------------------------------------------------------------------------------------------- 
        public void RXThread()
        {
            // Create new object containing received data 
            XLClass.xl_event receivedEvent = new XLClass.xl_event();

            // Result of XL Driver function calls
            XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;

            // Result values of WaitForSingleObject 
            XLDefine.WaitResults waitResult = new XLDefine.WaitResults();



            // Note: this thread will be destroyed by MAIN
            while (true)
            {
                // Wait for hardware events
                //waitResult = (XLDefine.WaitResults)WaitForSingleObject(eventHandle, 1000);

                // If event occurred...
                if (waitResult != XLDefine.WaitResults.WAIT_TIMEOUT)
                {
                    // ...init xlStatus first
                    xlStatus = XLDefine.XL_Status.XL_SUCCESS;

                    // afterwards: while hw queue is not empty...
                    while (xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY)
                    {
                        // ...block RX thread to generate RX-Queue overflows
                        while (blockRxThread) Thread.Sleep(1000);

                        // ...receive data from hardware.
                        xlStatus = CANDemo.XL_Receive(portHandle, ref receivedEvent);

                        //  If receiving succeed....
                        if (xlStatus == XLDefine.XL_Status.XL_SUCCESS)
                        {
                            if ((receivedEvent.flags & XLDefine.XL_MessageFlags.XL_EVENT_FLAG_OVERRUN) != 0)
                            {
                                Console.WriteLine("-- XL_EVENT_FLAG_OVERRUN --");
                            }

                            // ...and data is a Rx msg...
                            if (receivedEvent.tag == XLDefine.XL_EventTags.XL_RECEIVE_MSG)
                            {
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0)
                                {
                                    Console.WriteLine("-- XL_CAN_MSG_FLAG_OVERRUN --");
                                }

                                // ...check various flags
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                {
                                    Console.WriteLine("ERROR FRAME");
                                }

                                else if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                {
                                    Console.WriteLine("REMOTE FRAME");
                                }

                                else
                                {
                                    Console.WriteLine(CANDemo.XL_GetEventString(receivedEvent));
                                }
                            }
                        }
                    }
                }
                // No event occurred
            }
        }
    }
}
