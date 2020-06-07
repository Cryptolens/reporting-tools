# -*- coding: utf-8 -*-
"""
Created on Sun Jun  7 14:42:28 2020

@author: Artem Los
"""

import json

def get_api_token():
    
    try:
        with open('apitokens.json', 'r') as f :
            return json.loads(f.read())["GetWebAPILogToken"]
    except Exception:
        return ""
    
    