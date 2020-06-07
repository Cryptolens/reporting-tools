# -*- coding: utf-8 -*-
"""
Created on Sun Jun  7 14:40:12 2020

@author: Artem Los
"""

from datetime import datetime, timedelta, date

from licensing.models import *
from licensing.methods import Key, Helpers

import pandas as pd

from tools import *

month_back = int(datetime.datetime.timestamp(datetime.datetime.today() - datetime.timedelta(days=30)))

logs = []

ending_before=0

while True:
    res = Key.get_web_api_log(token=get_api_token(), order_by="Id descending", limit = 1000, ending_before=ending_before)
    
    if res[0] == None:
        break
    
    logs = logs + res[0]
    
    if res[0][-1]["time"] < month_back:
        break;
        
    ending_before = res[0][-1]["id"] 
    
logs = pd.DataFrame(logs)

logs = logs[logs["time"]>month_back]