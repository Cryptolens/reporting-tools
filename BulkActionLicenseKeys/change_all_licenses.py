"""
The script below will set the maximum number of machines to the value 5 for all license keys in all products.
This can be changed so that it only affects one product or you can change the action that is being performed.

More methods that you could call are listed at https://app.cryptolens.io/docs/api/v3/Key.
"""

from licensing.models import *
from licensing.methods import Key, Helpers, Message, Product, Customer, Data, AI

# Token with GetProducts, GetKeys and Machine Lock Limit permission is needed.
token = "please add your token"

products = Product.get_products(token)[0]
for product in products:
    print(product["id"])
    product_id = product["id"]
    
    page_count = 1
    while True:
        product_keys = Product.get_keys(token, product_id, page=page_count)
        
        for key in product_keys[0]:
            print(key["key"])
            print(Key.machine_lock_limit(token, product_id, key["key"], 5))
    
        if page_count > product_keys[2]["pageCount"]:
            break
    
        page_count += 1
    