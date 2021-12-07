import numpy as np
import pandas as pd
import json
import unicodedata
import warnings
import math
from keras import models
from keras import layers
warnings.filterwarnings("ignore")

def load_json_dict(path):
    file = open(path, "r", encoding='utf-8-sig')
    text = file.read()
    return json.loads(text)

def load_json(path):
    file = open(path, "r", encoding='utf-8-sig')
    text = file.read()
    return pd.json_normalize(json.loads(text))

def info_to_tokens(info):
    tokens = []
    scale = info['Scale']
    for line in info['LineInfoList']:
        x = round(line['X'] / scale,3)
        y = round(line['Y'] / scale,3)
        length = line['Length'] / scale
        sin_angle = line['SinAngle']
        cos_angle = math.sqrt(1-sin_angle*sin_angle)
        x2 = round(x+length * cos_angle,3)
        y2 = round(y+length * sin_angle,3)
        tokens.append((1,x,y,x2,y2))
    for circle in info['CircleInfoList']:
        x = round(circle['X'] / scale,3)
        y = round(circle['Y'] / scale,3)
        r = round(circle['Radius'] / scale,3)
        tokens.append((2,x,y,r,r))
    for text in info['TextInfoList']:
        x = round(text['X'] / scale,3)
        y = round(text['Y'] / scale,3)
        string = text['TextString']
        n = len(string)
        rein_flag = 1 if '%%13' in string or '@' in string else 0
        tokens.append((3,x,y,n,rein_flag))
    return tokens

def fill_tokens(tokens, n_token):
    if(len(tokens)>=n_token):
        return np.array(tokens[:n_token])
    else:
        length = len(tokens)
        tokens.extend([(0,0,0,0,0) for i in range(n_token-length)])
        return np.array(tokens)

def model_prediction():
    para_dict = load_json_dict("DetailComInfoPara.json")
    n_token = para_dict['n_token']
    label_dict = para_dict['label_dict']
    label_index_dict = para_dict['label_index_dict']

    info = load_json("DetailComInfoTemp.json").iloc[0]
    tokens = info_to_tokens(info)
    X_test = np.array([fill_tokens(tokens, n_token)])
    model = models.load_model("DetailComInfoModel.h5")

    result = model.predict(X_test)
    label_pred = label_index_dict[str(np.argmax(result))]
    prob_pred = np.max(result)
    print(label_pred, prob_pred)

if __name__ == '__main__':
    model_prediction()