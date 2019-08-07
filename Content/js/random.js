var _globalSeed = 1;
function hashCode(s) 
{
    for(var i = 0, h = 0; i < s.length; i++)
        h = Math.imul(31, h) + s.charCodeAt(i) | 0;
    return h;
}

function random() 
{
    var x = Math.sin(_globalSeed++) * 10000;
    return x - Math.floor(x);
}

function randomHSV(saturation, value, seed = null)
{
    if (seed != null && typeof seed === 'string') _globalSeed = hashCode(seed);
    else _globalSeed = seed || _globalSeed;

    hue = (random() * 1000) % 360;
    return "hsl(" + hue + ", " + (saturation * 100) + "%, " + (value * 100) + "%)";
}

function locationColor(location)
{
    if (location == null) return null;
    if (location.startsWith('InstanceWorld'))
        return '#7b7b7b';

    if (location.startsWith('ClientShipWorld'))
        return '#7b7b7b';

    return randomHSV(1, 0.70, location);
}