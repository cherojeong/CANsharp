using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANsharp
{
    public class CANdb
    {
        public Dictionary<String, Message> messages = new Dictionary<string,Message>();
        public Dictionary<String, Signal> signals = new Dictionary<string,Signal>();
        public Dictionary<String, List<String>> signalToMessages = new Dictionary<string,List<String>>();

        public void AddOnSignalToMessages(Signal sgn, Message msg) {
            List<String> list;
 
            if (signalToMessages.TryGetValue(sgn.Name, out list)) {
                list.Add(msg.Name);
            } else {
                list = new List<string>();
                list.Add(msg.Name);
                signalToMessages.Add(sgn.Name, list);
            }
        }

        public void AddPositionToMessage(Signal sgn, Message msg, int position)
        {
            String sgnName = sgn.Name;
            String msgname = msg.Name;

            Message message = messages[msgname];
            message.Position.Add(sgnName, position);
            message.Signals.Add(sgn.ToString());
        }

        public List<Signal> GetAllSignal()
        {
            List<String> keys = new List<String>(signals.Keys);
            List<Signal> list = new List<Signal>();

            foreach (String k in keys)
            {
                list.Add(signals[k]);
            }

            return list;
        }

        public List<Message> GetAllMessage()
        {
            List<String> keys = new List<String>(messages.Keys);
            List<Message> list = new List<Message>();

            foreach (String k in keys)
            {
                list.Add(messages[k]);
            }

            return list;

        }

        public void UpdateMessage(Signal signal)
        {
            List<String> list = signalToMessages[signal.Name];
            foreach (String s in list)
            {
                messages[s].Update(signal);
            }
        }

        public void UpdateSignalValue(String name, double value)
        {
            signals[name].Value = value;
            UpdateMessage(signals[name]);
        }

        public List<Message> GetMessageFromSignal(String name)
        {
            List<String> messageNames = signalToMessages[name];
            List<Message> messageList = new List<Message>();
            foreach (String s in messageNames)
            {
                messageList.Add(messages[s]);
            }
            return messageList;
        }

        public void PrintAll() {
            List<String> keys = new List<String>(messages.Keys);

            foreach (String k in keys) {
                Console.WriteLine(messages[k].ToString());
            }

            keys = new List<String>(signals.Keys);
            foreach (String k in keys)
            {
                Console.WriteLine(signals[k].ToString());
            }
        }
    }
}
