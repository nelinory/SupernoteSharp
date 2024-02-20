```
signature
header
	"MODULE_LABEL"
	"FILE_TYPE"
	"APPLY_EQUIPMENT"
	"FINALOPERATION_PAGE"
	"FINALOPERATION_LAYER"
	"ORIGINAL_STYLE"
	"ORIGINAL_STYLEMD5"
	"DEVICE_DPI"
	"SOFT_DPI"
	"FILE_PARSE_TYPE"
	"RATTA_ETMD"
	"APP_VERSION"
	"FILE_ID"
	"FILE_RECOGN_TYPE"
	"FILE_RECOGN_LANGUAGE"
    "PDFSTYLE" - "none" means no PDF template is used
    "PDFSTYLEMD5" - 0 means no PDF template is used
    "STYLEUSAGETYPE" - 0 means normal style (IMAGE), 2 is PDF template as style (PDF)
footer
	"PAGEXXXX" [..] - max pages 9999
	"TITLE_XXXX" [..]
	"KEYWORD_XXXX" [..]
	"COVER_X" - COVER_1 means there is cover, COVER_0 means no cover
	"DIRTY"
	"LINKO_XXXX" [..] - outbound links
	"LINKI_XXXX" [..] - inbound links
	"FILE_FEATURE"
    "PDFSTYLELIST"
	"STYLE_XXXX" [..]
	keywords []
		"KEYWORDPAGE"
		"KEYWORDSEQNO"
		"KEYWORDRECT"
		"KEYWORDRECTORI"
		"KEYWORDSITE"
	titles []
		"TITLESEQNO"
		"TITLELEVEL"
		"TITLERECT"
		"TITLERECTORI"
		"TITLEBITMAP"
		"TITLEPROTOCOL"
		"TITLESTYLE"
	links []
		"LINKTYPE" - 0 is "Page", 1 is "File", 4 is "Web"
		"LINKINOUT" - 0 is "Out", 1 is "In"
		"LINKBITMAP"
		"LINKSTYLE"
		"LINKTIMESTAMP"
		"LINKRECT"
		"LINKRECTORI"
		"LINKPROTOCAL"
		"LINKFILE" - Base64-encoded file path or URL
		"LINKFILEID"
		"PAGEID"
		"OBJPAGE"
pages
	"PAGESTYLE"
	"PAGESTYLEMD5"
	"LAYERINFO"
	"LAYERSEQ" - layer order of precedence is "LAYER3,LAYER2,LAYER1,MAINLAYER,BGLAYER"
	"MAINLAYER"
	"LAYER1"
	"LAYER2"
	"LAYER3"
	"BGLAYER"
	"TOTALPATH"
	"THUMBNAILTYPE"
	"RECOGNSTATUS"
	"RECOGNTEXT"
	"RECOGNFILE"
	"PAGEID"
	"RECOGNTYPE"
	"RECOGNFILESTATUS"
	"RECOGNLANGUAGE"
	"EXTERNALLINKINFO"
	"FIVESTAR" []
	layers [] - max 5 layers supported
		"LAYERTYPE"
		"LAYERPROTOCOL"
		"LAYERNAME"
		"LAYERPATH"
		"LAYERBITMAP"
		"LAYERVECTORGRAPH"
		"LAYERRECOGN"
```

[] - denotes array of objects  
[..] - denotes multiple items of the same type as PAGE1, PAGE2, etc.