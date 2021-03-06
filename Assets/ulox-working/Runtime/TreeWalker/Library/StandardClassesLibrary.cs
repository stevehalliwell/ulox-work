﻿namespace ULox
{
    public class StandardClassesLibrary : ILoxEngineLibraryBinder
    {
        public void BindToEngine(Engine engine)
        {
            engine.SetValue("POD", 
                new PODClass(null));
            engine.SetValue("Array", 
                new ArrayClass(null));
            engine.SetValue("List", 
                new ListClass(null));
        }
    }
}
