<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fo="http://www.w3.org/1999/XSL/Format" version="1.0">

    <xsl:import href="../docbook/xsl/fo/docbook.xsl"/>
    <xsl:import href="boxsocial.titlepage.xsl"/>

    <xsl:param name="fop.extensions">1</xsl:param>
    <xsl:param name="fop1.extensions">1</xsl:param>
    <xsl:param name="double.sided">1</xsl:param>
    <xsl:param name="tex.math.in.alt">1</xsl:param>
    <xsl:param name="shade.verbatim">1</xsl:param>
    <xsl:param name="use.svg">1</xsl:param>
    <xsl:param name="xep.extensions">1</xsl:param>
    
    <xsl:param name="title.margin.left">0cm</xsl:param>
    
    <xsl:param name="title.fontset">LMRoman17-Regular</xsl:param>
    <xsl:param name="title.fontsize">17pt</xsl:param>

    <xsl:param name="chunk.fast" select="1"/>
    <!-- Do NOT add the first section into the starting chunk -->
    <xsl:param name="chunk.first.sections" select="1"/>

    <!-- Enumerate sections -->
    <xsl:param name="section.autolabel" select="1"/>
    <!-- Include numebers of the top elements -->
    <xsl:param name="section.label.includes.component.label" select="1"/>

    <!-- We use the section / chapter ids as filenames -->
    <xsl:param name="use.id.as.filename" select="1"/>

    <!-- TOC -->
    <xsl:param name="toc.section.depth" select="4"/>

    <!--<xsl:template match="title" mode="book.titlepage.recto.auto.mode">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="52pt" space-before="22pt"
            font-weight="normal" font-family="Garamond">
            <xsl:apply-templates select="." mode="book.titlepage.recto.mode"/>
        </fo:block>
    </xsl:template-->

    <!--xsl:template match="author" mode="book.titlepage.recto.auto.mode">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="24pt" space-before="52pt"
            font-weight="normal" font-family="Garamond" font-style="italic"> by <xsl:apply-templates
                select="." mode="book.titlepage.recto.mode"/>
        </fo:block>
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="18pt" space-before="12pt"
            font-weight="normal" font-family="Garamond">
            <xsl:apply-templates select="affiliation/orgdiv" mode="titlepage.mode"/>,
                <xsl:apply-templates select="affiliation/orgname" mode="titlepage.mode"/>.
        </fo:block>
    </xsl:template-->

    <!--xsl:template match="cover" mode="book.titlepage.recto.auto.mode">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style"
            font-size="18pt"
            space-before="12pt"
            font-weight="normal"
            font-family="Garamond">
            ||<xsl:apply-templates select="." mode="book.titlepage.recto.mode"/>
            <xsl:apply-templates select="affiliation/orgdiv" mode="titlepage.mode"/>
        </fo:block>
    </xsl:template>
    
    <xsl:template match="cover" mode="titlepage.mode">
        <xsl:apply-templates mode="titlepage.mode"/>
        </xsl:template-->

    <!--xsl:template name="book.titlepage.recto">
        <xsl:choose>
            <xsl:when test="bookinfo/title">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="bookinfo/title"/>
            </xsl:when>
            <xsl:when test="info/title">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="info/title"/>
            </xsl:when>
            <xsl:when test="title">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="title"/>
            </xsl:when>
        </xsl:choose>

        <xsl:choose>
            <xsl:when test="bookinfo/subtitle">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode"
                    select="bookinfo/subtitle"/>
            </xsl:when>
            <xsl:when test="info/subtitle">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="info/subtitle"/>
            </xsl:when>
            <xsl:when test="subtitle">
                <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="subtitle"/>
            </xsl:when>
        </xsl:choose>

        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="bookinfo/corpauthor"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="info/corpauthor"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="bookinfo/authorgroup"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="info/authorgroup"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="bookinfo/author"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="info/author"/>
        <xsl:apply-templates mode="book.titlepage.recto.auto.mode" select="bookinfo/cover"/>
    </xsl:template-->

    <!--xsl:template match="cover" mode="book.titlepage.recto.auto.mode">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="16pt" space-before="12pt"
            font-weight="normal" font-family="Garamond">
            <xsl:apply-templates mode="titlepage.mode"/>
        </fo:block>
    </xsl:template-->

    <!--xsl:template match="title" mode="chapter.titlepage.recto.auto.mode">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="22pt" space-before="3pt"
            font-weight="normal" font-family="Garamond">
            <xsl:call-template name="component.title">
                <xsl:with-param name="node" select="ancestor-or-self::chapter[1]"/>
            </xsl:call-template>
        </fo:block>
    </xsl:template-->

    <xsl:template match="title" mode="section.level1.title.properties">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
            xsl:use-attribute-sets="book.titlepage.recto.style" font-size="18pt" space-before="1pt"
            font-weight="normal" font-family="Garamond"> </fo:block>
    </xsl:template>

    <xsl:template name="table.of.contents.titlepage.recto">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format" font-size="18pt" space-before="1pt"
            font-weight="normal" font-family="Garamond">
            <xsl:call-template name="gentext">
                <xsl:with-param name="key" select="'TableofContents'"/>
            </xsl:call-template>
        </fo:block>
    </xsl:template>

    <xsl:template name="list.of.figures.titlepage.recto">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format" font-size="18pt" space-before="1pt"
            font-weight="normal" font-family="Garamond">
            <xsl:call-template name="gentext">
                <xsl:with-param name="key" select="'ListofFigures'"/>
            </xsl:call-template>
        </fo:block>
    </xsl:template>

    <xsl:template name="list.of.tables.titlepage.recto">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format" font-size="18pt" space-before="1pt"
            font-weight="normal" font-family="Garamond">
            <xsl:call-template name="gentext">
                <xsl:with-param name="key" select="'ListofTables'"/>
            </xsl:call-template>
        </fo:block>
    </xsl:template>
    
    <xsl:template name="list.of.equations.titlepage.recto">
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format" font-size="18pt" space-before="1pt"
            font-weight="normal" font-family="Garamond">
            <xsl:call-template name="gentext">
                <xsl:with-param name="key" select="'ListofEquations'"/>
            </xsl:call-template>
        </fo:block>
    </xsl:template>

    <xsl:attribute-set name="formal.title.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">12pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>

    <xsl:attribute-set name="component.title.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">22pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>

    <xsl:attribute-set name="section.title.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">22pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>

    <xsl:attribute-set name="section.title.level1.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">18pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
        <!--xsl:attribute name="break-before">page</xsl:attribute-->
    </xsl:attribute-set>

    <xsl:attribute-set name="section.title.level2.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">16pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>

    <xsl:attribute-set name="section.title.level3.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">14pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>

    <xsl:template name="preface.titlepage.recto">

        <xsl:if test="@id = 'ack' or @id = 'ab'">
            <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format"
                xsl:use-attribute-sets="preface.titlepage.recto.style"
                margin-left="{$title.margin.left}" font-size="24.8832pt"
                font-family="{$title.fontset}" font-weight="bold">
                <xsl:call-template name="component.title">
                    <xsl:with-param name="node" select="ancestor-or-self::preface[1]"/>
                </xsl:call-template>
            </fo:block>
        </xsl:if>

        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/corpauthor"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/corpauthor"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/corpauthor"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/authorgroup"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/authorgroup"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/authorgroup"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="prefaceinfo/author"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/author"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/author"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/othercredit"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/othercredit"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/othercredit"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/releaseinfo"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/releaseinfo"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/releaseinfo"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="prefaceinfo/copyright"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/copyright"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/copyright"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/legalnotice"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/legalnotice"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/legalnotice"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="prefaceinfo/pubdate"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/pubdate"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/pubdate"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="prefaceinfo/revision"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/revision"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/revision"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode"
            select="prefaceinfo/revhistory"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/revhistory"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/revhistory"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="prefaceinfo/abstract"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="docinfo/abstract"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/abstract"/>
        <xsl:apply-templates mode="preface.titlepage.recto.auto.mode" select="info/cover"/>

    </xsl:template>

    <xsl:param name="formal.title.placement"> figure after example after equation after table before
    procedure before task before </xsl:param>
    
    <!--xsl:param name="body.margin.top">3cm</xsl:param>
    <xsl:param name="body.margin.bottom">3cm</xsl:param>
    <xsl:param name="region.before.extent">4cm</xsl:param-->
    
    <xsl:param name="header.rule" select="1"></xsl:param>
    <xsl:param name="footer.rule" select="1"></xsl:param>
    <xsl:param name="header.column.widths">1 1 1</xsl:param>
    <xsl:param name="header.table.height">14pt</xsl:param>
    
    <xsl:attribute-set name="header.content.properties">
        <xsl:attribute name="font-weight">normal</xsl:attribute>
        <xsl:attribute name="font-size">12pt</xsl:attribute>
        <xsl:attribute name="font-family">Garamond</xsl:attribute>
    </xsl:attribute-set>
    
    <xsl:param name="page.margin.inner">
        <xsl:choose>
            <xsl:when test="$double.sided != 0">2.0cm</xsl:when>
            <xsl:otherwise>2.0cm</xsl:otherwise>
        </xsl:choose>
    </xsl:param>
    
    <xsl:param name="page.margin.outer">
        <xsl:choose>
            <xsl:when test="$double.sided != 0">2.0cm</xsl:when>
            <xsl:otherwise>2.0cm</xsl:otherwise>
        </xsl:choose>
    </xsl:param>

    <!--xsl:param name="marker.section.level">2</xsl:param-->
    
    <!--xsl:template match="para[@role = 'clause']">
        <fo:block xsl:use-attribute-sets="normal.para.spacing">
            <xsl:call-template name="anchor"/>
            <xsl:number count="para[@role = 'clause']" level="any"/>
            <xsl:text>. </xsl:text>
            <xsl:apply-templates/>
        </fo:block>
    </xsl:template-->
    
    <xsl:template match="para[parent::section or parent::chapter]">
        <xsl:variable name="id"><xsl:call-template name="object.id"/></xsl:variable>
        <fo:list-block xsl:use-attribute-sets="list.block.spacing"
            provisional-distance-between-starts="2em"
            provisional-label-separation="0.2em"
            start-indent="10pt">
            <fo:list-item xsl:use-attribute-sets="list.item.spacing">
        <fo:list-item-label end-indent="label-end()" xsl:use-attribute-sets="itemizedlist.label.properties">
            <fo:block xsl:use-attribute-sets="normal.para.spacing">
        <!--fo:block -->
            <xsl:call-template name="anchor"/>
            <xsl:if test="parent::section">
                <xsl:apply-templates select="parent::section" mode="label.markup"/>
                <xsl:text>.</xsl:text>
            </xsl:if>
            <xsl:number from="section" count="para[parent::section or parent::chapter]" level="any"/>
            <xsl:text>. </xsl:text>
            </fo:block>
        </fo:list-item-label>
            <fo:list-item-body start-indent="50pt">
                <fo:block>
            <xsl:apply-templates/>
                </fo:block>
            </fo:list-item-body>
        </fo:list-item>
        </fo:list-block>
    </xsl:template>
       
    <xsl:template name="header.content">
        <xsl:param name="pageclass" select="''"/>
        <xsl:param name="sequence" select="''"/>
        <xsl:param name="position" select="''"/>
        <xsl:param name="gentext-key" select="''"/>
        <fo:block xmlns:fo="http://www.w3.org/1999/XSL/Format" font-size="12pt" space-before="0pt"
            font-weight="normal" font-family="Garamond">
            <xsl:choose>
                <xsl:when test="$sequence != 'first' and $sequence != 'blank' and $position = 'center'">
                    <xsl:apply-templates select="." mode="titleabbrev.markup"/>
                    <!--fo:retrieve-marker retrieve-class-name="section.head.marker"
                        retrieve-position="first-including-carryover"
                        retrieve-boundary="page-sequence"/-->
                </xsl:when>
            </xsl:choose>
        </fo:block>
    </xsl:template>
    
    <xsl:template name="is.graphic.format">
        <xsl:param name="format"></xsl:param>
        <xsl:if test="$format = 'SVG'
            or $format = 'PNG'
            or $format = 'JPG'
            or $format = 'JPEG'
            or $format = 'linespecific'
            or $format = 'GIF'
            or $format = 'GIF87a'
            or $format = 'GIF89a'
            or $format = 'BMP'
            or $format = 'SWF'">1</xsl:if>
    </xsl:template>
    
    <xsl:template match="processing-instruction('hard-pagebreak')">
        <fo:block break-before='page'/>
    </xsl:template>
    
    <xsl:template name="initial.page.number">
        <xsl:param name="element" select="local-name(.)"/>
        <xsl:param name="master-reference" select="''"/>
        <xsl:choose>
            <xsl:when test="$element = 'chapter' and not(preceding::chapter)">1</xsl:when>
            <xsl:otherwise>auto-odd</xsl:otherwise>
        </xsl:choose>
    </xsl:template>

</xsl:stylesheet>
