<?xml version="1.0" encoding="utf-8" ?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Ian MacLean (ian@maclean.ms)
// Scott Hernandez (ScottHernandez-at-Hotmail....com)
-->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    <xsl:include href="tags.xslt" />
    <xsl:include href="common.xslt" />
    <xsl:include href="nant-attributes.xslt" />
    
    <xsl:output method="html" indent="yes" />

    <!-- The class we are documenting this time. This value will be passed in by the caller. argv[] equivalent. Default value is used for testing -->
    <xsl:param name="functionName">T:NAnt.Core.Types.FileSet</xsl:param>

    <!-- helper values for adjusting the paths -->
    <xsl:param name="refType">Function</xsl:param>

    <xsl:template match="/">
        <html>
            <xsl:comment> Documenting <xsl:value-of select="$functionName"/> </xsl:comment>
            <xsl:apply-templates select="//method[@name=$functionName]" mode="FunctionDoc"/>
        </html>
    </xsl:template>
    
    <xsl:template match="method" mode="FunctionDoc">
        <xsl:variable name="name"><xsl:value-of select="@name" /></xsl:variable>
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
            <link rel="stylesheet" type="text/css" href="../../style.css" />
            <title><xsl:value-of select="$name" /> Function</title>
        </head>
        <body>
            <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
                <tr>
                    <td class="NavBar-Cell" width="100%">
                        <a href="../../index.html"><b>NAnt</b></a>
                        <img alt="->" src="../images/arrow.gif" />
                        <a href="../index.html">Help</a>
                        <img alt="->" src="../images/arrow.gif" />
                        <a href="../functions.html">Function Reference</a>
                        <img alt="->" src="../images/arrow.gif" /><xsl:text> </xsl:text>
                        <xsl:value-of select="$name" /> Function
                    </td>
                </tr>
            </table>
    
            <h1><xsl:value-of select="$name" /> Function</h1>
            <xsl:apply-templates select="."/>
        </body>

    </xsl:template>

    <!-- match class tag for info about a type -->
    <xsl:template match="method">
    
        <!-- output whether type is deprecated -->
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']"/>
        <xsl:if test="count($ObsoleteAttribute) > 0">
            <p>
                <i>(Deprecated)</i>
            </p>
        </xsl:if>
        
        <p><xsl:apply-templates select="documentation/summary" mode="slashdoc"/></p>

        <h3>Usage</h3>
        <code>
            <xsl:value-of select="@name" />(<xsl:for-each select="parameter"><xsl:if test="position() != 1">, </xsl:if><xsl:value-of select="@name" /></xsl:for-each>)
        </code>
        <p/>
        
        <xsl:if test="count(parameter) != 0">
            <h3>Parameters</h3>
            <div class="table">
                <table>
                    <tr>
                        <th>Name</th>
                        <th>Type</th>
                        <th>Description</th>
                    </tr>
                    <xsl:for-each select="parameter">
                        <tr>
                            <td><xsl:value-of select="@name" /></td>
                            <td><xsl:value-of select="@type" /></td>
                            <xsl:variable name="paramname" select="@name" />
                            <td><xsl:apply-templates select="../documentation/param[@name=$paramname]" mode="slashdoc" /></td>
                        </tr>
                    </xsl:for-each>
                </table>
            </div>
        </xsl:if>
        <h3>Return Value</h3>
        <xsl:apply-templates select="documentation/returns" mode="slashdoc"/>
        <xsl:if test="count(documentation/remarks) != 0">
            <h3>Remarks</h3>
            <xsl:apply-templates select="documentation/remarks" mode="slashdoc"/>
        </xsl:if>
        <xsl:if test="count(documentation/example) != 0">
            <h3>Examples</h3>
            <xsl:apply-templates select="documentation/example" mode="slashdoc"/>
        </xsl:if>
    </xsl:template>

</xsl:stylesheet>
