"""Original tileable surface wear; deterministic, no downloaded textures."""
import math, struct, zlib, random, sys
from pathlib import Path

def main(output):
 output=Path(output);output.mkdir(parents=True,exist_ok=True)
 size=256
 for name in ['Wear','Fabric']:
  random.seed(517);pixels=bytearray()
  for y in range(size):
   pixels.append(0)
   for x in range(size):
    u=x/size*math.tau;v=y/size*math.tau
    cloud=(math.sin(u*3+math.sin(v*2))*.32+math.cos(v*4+math.sin(u))*.26+math.sin(u*11+v*9)*.08)
    if name=='Wear':
     stain=max(0,cloud-.08)*.7
     grain=random.random()*.08
     value=.98-stain-grain
     if cloud<-.30 and random.random()<.14:value-=.17
    else:
     weave=.05*((x%3==0)+(y%3==0))
     value=.94-weave+cloud*.1-random.random()*.04
    c=int(max(0,min(1,value))*255);pixels.extend((c,c,c))
  def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
  encoded=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(pixels,9))+chunk(b'IEND',b'')
  (output/('TEX_Hospital_'+name+'.png')).write_bytes(encoded)

if __name__=='__main__':main(sys.argv[1] if len(sys.argv)>1 else Path(__file__).resolve().parents[2]/'Unity/Assets/SpookTuber/Hospital/Textures')
