
import json
import mmap
import optparse
import starbound
import sys
from pprint import pprint

import starbound

if len(sys.argv) != 2:
    print ("{}")
    exit
    
else:

    path = sys.argv[1]
    with open(path, 'r+b') as fh:
        mm = mmap.mmap(fh.fileno(), 0, access=mmap.ACCESS_READ)
        world = starbound.World(mm)
        world.read_metadata()

        dmp = {
            'seed': world.metadata['worldTemplate']['seed'],
            'spawn': world.metadata['playerStart'],
            'size': world.metadata['worldTemplate']['size'],
            'celestial': world.metadata['worldTemplate']['celestialParameters'],
            'sky': world.metadata['worldTemplate']['skyParameters'],
            'world': world.metadata['worldTemplate']['worldParameters']
        }

        #player = starbound.read_sbvj01(fh)
        print(json.dumps(dmp))