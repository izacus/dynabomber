using System.Windows;
using System.Windows.Shapes;

namespace DynaBomberClient
{

    abstract public class GameObject
    {
        protected bool inUse;
        protected Point position = new Point(0, 0);

        public bool InUse
        {
            get
            {
                return inUse;
            }
        }

        public Point Position
        {
            get { return position; }
            set { position = value; }
        }

        virtual public void Collide(Rectangle other)
        {            
        }

        virtual public void Collide(GameObject other)
        {
        }

        virtual public Rectangle GetRectangle()
        {
            return null;
        }

        virtual public void ShutDown()
        {
            //destroys the object
        }

        public virtual void Display()
        {
        }
    }
}
