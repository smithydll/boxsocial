@ECHO OFF
SET XML_CATALOG_FILES=C:\SVN\BoxSocial\docs\docbook\5.0
rem book
libxml\xsltproc.exe --output xsl\boxsocial.titlepage.xsl docbook\xsl\template\titlepage.xsl xsl\boxsocial.titlepage.xml
libxml\xsltproc.exe --xinclude --xincludestyle --stringparam fop1.extensions 1 --stringparam paper.type A4 --output book.fo xsl\boxsocial.xsl book.xml
rem del book.pdf
fop\fop book.fo book.pdf
rem del book.fo

rem R1.1-2008
libxml\xsltproc.exe --xinclude --xincludestyle --stringparam fop1.extensions 1 --stringparam paper.type A4 --output R1.1-2008.fo xsl\boxsocial.xsl R\R1\R1.1-2008.xml
rem del R1.1-2008.pdf
fop\fop R1.1-2008.fo R1.1-2008.pdf
rem del R1.1-2008.fo
