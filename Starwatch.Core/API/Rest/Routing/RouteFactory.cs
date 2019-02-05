using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Starwatch.API.Rest.Routing.Exceptions;
using Starwatch.API.Rest.Serialization;

namespace Starwatch.API.Rest.Routing
{
    public class RouteFactory
    {
        public RouteAttribute RouteAttribute { get; }

        public AuthLevel AuthenticationLevel => RouteAttribute.Permission;
        public string Route => RouteAttribute.Route;
        public string BaseRoute { get; }
        public int SegmentCount { get; }

        public Type RouteType { get; }

        private ArgumentMap[] _argmap;
        private struct ArgumentMap
        {
            public int index;
            public string name;
            public PropertyInfo property;
            public ArgumentAttribute argumentAttribute;
        }

        public RouteFactory(RouteAttribute routeAttribute, Type routeType)
        {
            RouteAttribute = routeAttribute;
            RouteType = routeType;


            //Seperate the segments
            string[] segments = routeAttribute.GetSegments();
            Debug.Assert(segments.Length >= 1);

            //Setup teh segment count and base route
            SegmentCount = segments.Length;
            BaseRoute = segments[0];

            //Create a temporary listing of mappings then find all the variables.
            Dictionary<string, ArgumentMap> mapping = new Dictionary<string, ArgumentMap>();
            for(int i = 0; i < segments.Length; i++)
            {
                Debug.Assert(segments[i].Length >= 2, "Segments need to be at least 2 characters long.");
                if (segments[i].Length >= 2 && segments[i][0] == routeAttribute.ArgumentPrefix)
                {
                    string name = segments[i].Substring(1);
                    mapping.Add(name, new ArgumentMap() { index = i, name = segments[i].Substring(1) });
                }
            }
            
            //Prepare the eventual setters
            foreach (var property in RouteType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<ArgumentAttribute>();
                if (attribute == null) continue;

                //Get the mapping
                ArgumentMap map;
                if (!mapping.TryGetValue(attribute.ArgumentName, out map))
                    throw new PropertyMappingException(property, attribute.ArgumentName, "Route does not contain any matching arguments.");

                //Some asserts to save our skin while developing
                Debug.Assert(property.CanWrite, "Argument properties must be writable");

                //Make sure it can write
                if (!property.CanWrite)
                    throw new PropertyMappingException(property, attribute.ArgumentName, "Argument properties must be writable");
                
                //Update the values
                map.property = property;
                map.argumentAttribute = attribute;
                mapping[attribute.ArgumentName] = map;
            }

            //Convert the dictionary into a flat array as that is all we need
            _argmap = mapping.Values.ToArray();
        }

        /// <summary>
        /// Calculates a score representing how close the segments match this route. The higher the score the better.
        /// </summary>
        /// <param name="stubs"></param>
        /// <returns></returns>
        public int CalculateRouteScore(string[] segments)
        {
            string[] selfSegments = RouteAttribute.GetSegments();

            //Make sure its all correct
            Debug.Assert(selfSegments.Length == segments.Length);
            if (selfSegments.Length != segments.Length) return 0;

            //Caculate the score
            int score = 0;
            for (int i = selfSegments.Length - 1; i >= 0; i--)
            {
                if (selfSegments[i].Equals(segments[i])) score += 10;                       //We match exectly, so bonus points
                else if (selfSegments[i][0] == RouteAttribute.ArgumentPrefix) score += 1;   //We match in the arguments, so some points
                else return 0;                                                              //We dont match at all, so abort while we are ahead.
            }

            //Return the calculated score.
            return score;
        }

        public RestRoute Create(RestHandler handler, Authentication auth, string[] segments)
        {
            Debug.Assert(segments.Length == SegmentCount);

            //Get the constructor of the type
            var info = RouteType.GetConstructor(new Type[] { typeof(RestHandler), typeof(Authentication) });
            Debug.Assert(info != null, "No matching constructor found");

            //Create an instance and register it
            var route = Activator.CreateInstance(RouteType, handler, auth) as RestRoute;
            Debug.Assert(route != null, "Failed to instance");

            //Setup its values
            foreach(var map in _argmap)
            {
                string valueText = segments[map.index];
                object value = null;
                
                if (map.argumentAttribute.Converter != null)
                {
                    //Use the serializer to convert
                    var converterType = map.argumentAttribute.Converter;
                    var converterInfo = converterType.GetConstructor(new Type[0]);
                    var converter = Activator.CreateInstance(converterType) as IArgumentConverter;

                    //Make sure its a valid conversion
                    Debug.Assert(converter != null);
                    if (!converter.TryConvertArgument(handler, valueText, out value))
                        throw new ArgumentMappingException($"Cannot convert '{valueText}' to {map.property.PropertyType} with converter {converter.GetType().FullName}!");
                    
                }
                else
                {
                    if (map.property.PropertyType == typeof(string))
                    {
                        //Its a string so literally just do string things
                        value = valueText;
                    }
                    else
                    {
                        //Do a stock standard convert
                        try
                        {
                            TypeConverter typeConverter = TypeDescriptor.GetConverter(map.property.PropertyType);
                            value = typeConverter.ConvertFromInvariantString(valueText);
                        }
                        catch (NotSupportedException e)
                        {
                            //We cannot convert it
                            throw new ArgumentMappingException($"Cannot convert '{valueText}' to {map.property.PropertyType} with stardard converter: {e.Message}");
                        }
                    }
                }

                //Set the value (making sure we can actually do that first
                Debug.Assert(map.property.CanWrite);

                //Check if the value is null
                if (value == null)
                {
                    switch (map.argumentAttribute.NullValueBehaviour)
                    {
                        default:
                        case ArgumentAttribute.NullBehaviour.Allow:
                            map.property.SetValue(route, value);
                            break;

                        case ArgumentAttribute.NullBehaviour.Ignore:
                            break;

                        case ArgumentAttribute.NullBehaviour.ReturnMissingResource:
                            throw new ArgumentMissingResourceException(map.name);
                    }
                }
                else
                {
                    map.property.SetValue(route, value);
                }
            }

            //return the object
            return route;
        }   
    }
}
