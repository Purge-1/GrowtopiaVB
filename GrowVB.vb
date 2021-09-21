Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports ENet.Managed
Imports Newtonsoft.Json.Linq

Namespace GrowtopiaServer
    Class GrowtopiaServer
        Public Shared server As ENetHost
        Public Shared cId As Integer = 1
        Public Shared itemsDat As Byte()
        Public Shared itemsDatSize As Integer = 0
        Public Shared peers As List(Of ENetPeer) = New List(Of ENetPeer)()
        Public Shared worldDB As WorldDB
        Public Shared itemDefs As ItemDefinition() = New ItemDefinition() {}
        Public droppedItems As DroppedItem() = New DroppedItem() {}
        Public Shared admins As Admin() = New Admin() {}

        Public Shared Function verifyPassword(ByVal password As String, ByVal hash As String) As Boolean
            Return hashPassword(password) = hash
        End Function

        Public Shared Function hashPassword(ByVal password As String) As String
            Using sha256Hash As SHA256 = SHA256.Create()
                Dim bytes As Byte() = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                Dim builder As StringBuilder = New StringBuilder()

                For i As Integer = 0 To bytes.Length - 1
                    builder.Append(bytes(i).ToString("x2"))
                Next

                Return builder.ToString()
            End Using
        End Function

        Public Shared Sub sendData(ByVal peer As ENetPeer, ByVal num As Integer, ByVal data As Byte(), ByVal len As Integer)
            Dim packet As Byte() = New Byte(len + 5 - 1) {}
            Array.Copy(BitConverter.GetBytes(num), 0, packet, 0, 4)

            If data IsNot Nothing Then
                Array.Copy(data, 0, packet, 4, len)
            End If

            packet(4 + len) = 0
            peer.Send(packet, 0, ENetPacketFlags.Reliable)
            server.Flush()
        End Sub

        Public Function getPacketId(ByVal data As Byte()) As Integer
            Return data(0)
        End Function

        Public Function getPacketData(ByVal data As Byte()) As Byte()
            Return data.Skip(4).ToArray()
        End Function

        Public Function text_encode(ByVal text As String) As String
            Dim ret As String = ""
            Dim i As Integer = 0

            While text(i) <> 0

                Select Case text(i)
                    Case vbLf
                        ret += "\n"
                    Case vbTab
                        ret += "\t"
                    Case vbBack
                        ret += "\b"
                    Case "\"c
                        ret += "\\"
                    Case vbCr
                        ret += "\r"
                    Case Else
                        ret += text(i)
                End Select

                i += 1
            End While

            Return ret
        End Function

        Public Shared Function ch2n(ByVal x As Char) As Byte
            Select Case x
                Case "0"c
                    Return 0
                Case "1"c
                    Return 1
                Case "2"c
                    Return 2
                Case "3"c
                    Return 3
                Case "4"c
                    Return 4
                Case "5"c
                    Return 5
                Case "6"c
                    Return 6
                Case "7"c
                    Return 7
                Case "8"c
                    Return 8
                Case "9"c
                    Return 9
                Case "A"c
                    Return 10
                Case "B"c
                    Return 11
                Case "C"c
                    Return 12
                Case "D"c
                    Return 13
                Case "E"c
                    Return 14
                Case "F"c
                    Return 15
            End Select

            Return 0
        End Function

        Public Shared Function explode(ByVal delimiter As String, ByVal str As String) As String()
            Return str.Split(delimiter.ToCharArray())
        End Function

        Public Structure GamePacket
            Public data As Byte()
            Public len As Integer
            Public indexes As Integer
        End Structure

        Public Shared Function appendFloat(ByVal p As GamePacket, ByVal val As Single) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + 4 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim num As Byte() = BitConverter.GetBytes(val)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 1
            Array.Copy(num, 0, data, p.len + 2, 4)
            p.len = p.len + 2 + 4
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function appendFloat(ByVal p As GamePacket, ByVal val As Single, ByVal val2 As Single) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + 8 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim fl1 As Byte() = BitConverter.GetBytes(val)
            Dim fl2 As Byte() = BitConverter.GetBytes(val2)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 3
            Array.Copy(fl1, 0, data, p.len + 2, 4)
            Array.Copy(fl2, 0, data, p.len + 6, 4)
            p.len = p.len + 2 + 8
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function appendFloat(ByVal p As GamePacket, ByVal val As Single, ByVal val2 As Single, ByVal val3 As Single) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + 12 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim fl1 As Byte() = BitConverter.GetBytes(val)
            Dim fl2 As Byte() = BitConverter.GetBytes(val2)
            Dim fl3 As Byte() = BitConverter.GetBytes(val3)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 3
            Array.Copy(fl1, 0, data, p.len + 2, 4)
            Array.Copy(fl2, 0, data, p.len + 6, 4)
            Array.Copy(fl3, 0, data, p.len + 10, 4)
            p.len = p.len + 2 + 12
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function appendInt(ByVal p As GamePacket, ByVal val As Int32) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + 4 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim num As Byte() = BitConverter.GetBytes(val)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 9
            Array.Copy(num, 0, data, p.len + 2, 4)
            p.len = p.len + 2 + 4
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function appendIntx(ByVal p As GamePacket, ByVal val As Int32) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + 4 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim num As Byte() = BitConverter.GetBytes(val)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 5
            Array.Copy(num, 0, data, p.len + 2, 4)
            p.len = p.len + 2 + 4
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function appendString(ByVal p As GamePacket, ByVal str As String) As GamePacket
            Dim data As Byte() = New Byte(p.len + 2 + str.Length + 4 - 1) {}
            Array.Copy(p.data, 0, data, 0, p.len)
            Dim strn As Byte() = Encoding.ASCII.GetBytes(str)
            data(p.len) = CByte(p.indexes)
            data(p.len + 1) = 2
            Dim len As Byte() = BitConverter.GetBytes(str.Length)
            Array.Copy(len, 0, data, p.len + 2, 4)
            Array.Copy(strn, 0, data, p.len + 6, str.Length)
            p.len = p.len + 2 + str.Length + 4
            p.indexes += 1
            p.data = data
            Return p
        End Function

        Public Shared Function createPacket() As GamePacket
            Dim data As Byte() = New Byte(60) {}
            Dim asdf As String = "0400000001000000FFFFFFFF00000000080000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"

            For i As Integer = 0 To asdf.Length - 1 Step 2
                Dim x As Byte = ch2n(asdf(i))
                                ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 				x = (byte)(x << 4);

''' 
                x += ch2n(asdf(i + 1))
                data(i / 2) = x
                If asdf.Length > 61 * 2 Then Throw New Exception("?")
            Next

            Dim packet As GamePacket
            packet.data = data
            packet.len = 61
            packet.indexes = 0
            Return packet
        End Function

        Public Shared Function packetEnd(ByVal p As GamePacket) As GamePacket
            Dim n As Byte() = New Byte(p.len + 1 - 1) {}
            Array.Copy(p.data, 0, n, 0, p.len)
            p.data = n
            p.data(p.len) = 0
            p.len += 1
            p.data(56) = CByte(p.indexes)
            p.data(60) = CByte(p.indexes)
            Return p
        End Function

        Public Structure InventoryItem
            Public itemID As Int16
            Public itemCount As Byte
        End Structure

        Public Class PlayerInventory
            Public items As InventoryItem() = New InventoryItem() {}
            Public inventorySize As Integer = 100
        End Class

        Public Class PlayerInfo
            Public isIn As Boolean = False
            Public netID As Integer
            Public haveGrowId As Boolean = False
            Public tankIDName As String = ""
            Public tankIDPass As String = ""
            Public requestedName As String = ""
            Public rawName As String = ""
            Public displayName As String = ""
            Public country As String = ""
            Public adminLevel As Integer = 0
            Public currentWorld As String = "EXIT"
            Public radio As Boolean = True
            Public x As Integer
            Public y As Integer
            Public isRotatedLeft As Boolean = False
            Public isUpdating As Boolean = False
            Public joinClothesUpdated As Boolean = False
            Public cloth_hair As Integer = 0
            Public cloth_shirt As Integer = 0
            Public cloth_pants As Integer = 0
            Public cloth_feet As Integer = 0
            Public cloth_face As Integer = 0
            Public cloth_hand As Integer = 0
            Public cloth_back As Integer = 0
            Public cloth_mask As Integer = 0
            Public cloth_necklace As Integer = 0
            Public canWalkInBlocks As Boolean = False
            Public canDoubleJump As Boolean = False
            Public isInvisible As Boolean = False
            Public noHands As Boolean = False
            Public noEyes As Boolean = False
            Public noBody As Boolean = False
            Public devilHorns As Boolean = False
            Public goldenHalo As Boolean = False
            Public isFrozen As Boolean = False
            Public isCursed As Boolean = False
            Public isDuctaped As Boolean = False
            Public haveCigar As Boolean = False
            Public isShining As Boolean = False
            Public isZombie As Boolean = False
            Public isHitByLava As Boolean = False
            Public haveHauntedShadows As Boolean = False
            Public haveGeigerRadiation As Boolean = False
            Public haveReflector As Boolean = False
            Public isEgged As Boolean = False
            Public havePineappleFloag As Boolean = False
            Public haveFlyingPineapple As Boolean = False
            Public haveSuperSupporterName As Boolean = False
            Public haveSupperPineapple As Boolean = False
            Public skinColor As UInteger = &H8295C3FF
            Public inventory As PlayerInventory = New PlayerInventory()
            Public lastSB As Long = 0
        End Class

        Public Shared Function getState(ByVal info As PlayerInfo) As Integer
            Dim val As Integer = 0
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.canWalkInBlocks ? 1 : 0 << 0;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.canDoubleJump ? 1 : 0 << 1;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.isInvisible ? 1 : 0 << 2;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.noHands ? 1 : 0 << 3;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.noEyes ? 1 : 0 << 4;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.noBody ? 1 : 0 << 5;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.devilHorns ? 1 : 0 << 6;

''' 
                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitConditionalExpression(ConditionalExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 			val |= info.goldenHalo ? 1 : 0 << 7;

''' 
            Return val
        End Function

        Public Structure WorldItem
            Public foreground As Int16
            Public background As Int16
            Public breakLevel As Integer
            Public breakTime As Long
            Public water As Boolean
            Public fire As Boolean
            Public glue As Boolean
            Public red As Boolean
            Public green As Boolean
            Public blue As Boolean
        End Structure

        Public Class WorldInfo
            Public width As Integer = 100
            Public height As Integer = 60
            Public name As String = "TEST"
            Public items As WorldItem()
            Public owner As String = ""
            Public isPublic As Boolean = False
        End Class

        Public Shared Function generateWorld(ByVal name As String, ByVal width As Integer, ByVal height As Integer) As WorldInfo
            Dim world As WorldInfo = New WorldInfo()
            Dim rand As Random = New Random()
            world.name = name
            world.width = width
            world.height = height
            world.items = New WorldItem(world.width * world.height - 1) {}

            For i As Integer = 0 To world.width * world.height - 1

                If i >= 3800 AndAlso i < 5400 AndAlso rand.[Next](0, 50) = 0 Then
                    world.items(i).foreground = 10
                ElseIf i >= 3700 AndAlso i < 5400 Then
                    world.items(i).foreground = 2
                ElseIf i >= 5400 Then
                    world.items(i).foreground = 8
                End If

                If i >= 3700 Then world.items(i).background = 14

                If i = 3650 Then
                    world.items(i).foreground = 6
                ElseIf i >= 3600 AndAlso i < 3700 Then
                    world.items(i).foreground = 0
                End If

                If i = 3750 Then world.items(i).foreground = 8
            Next

            Return world
        End Function

        Public Class PlayerDB
            Public Shared Function getProperName(ByVal name As String) As String
                Dim newS As String = name.ToLower()
                Dim ret = New StringBuilder()

                For i As Integer = 0 To newS.Length - 1

                    If newS(i) = "`"c Then
                        i += 1
                    Else
                        ret.Append(newS(i))
                    End If
                Next

                Dim ret2 = New StringBuilder()

                For Each c As Char In ret.ToString()
                    If (c >= "a"c AndAlso c <= "z"c) OrElse (c >= "0"c AndAlso c <= "9"c) Then ret2.Append(c)
                Next

                Return ret2.ToString()
            End Function

            Public Shared Function fixColors(ByVal text As String) As String
                Dim ret As String = ""
                Dim colorLevel As Integer = 0

                For i As Integer = 0 To text.Length - 1

                    If text(i) = "`"c Then
                        ret += text(i)
                        If i + 1 < text.Length Then ret += text(i + 1)

                        If i + 1 < text.Length AndAlso text(i + 1) = "`"c Then
                            colorLevel -= 1
                        Else
                            colorLevel += 1
                        End If

                        i += 1
                    Else
                        ret += text(i)
                    End If
                Next

                For i As Integer = 0 To colorLevel - 1
                    ret += "``"
                Next

                For i As Integer = 0 To colorLevel + 1
                    ret += "`w"
                Next

                Return ret
            End Function

            Public Shared Function playerLogin(ByVal peer As ENetPeer, ByVal username As String, ByVal password As String) As Integer
                Dim path As String = "players/" & getProperName(username) & ".json"

                If File.Exists(path) Then
                    Dim j As JObject = JObject.Parse(File.ReadAllText(path))
                    Dim pss As String = CStr(j("password"))

                    If verifyPassword(password, pss) Then

                        For Each currentPeer As ENetPeer In peers
                            If currentPeer.State <> ENetPeerState.Connected Then Continue For
                            If currentPeer = peer Then Continue For

                            If (TryCast(currentPeer.Data, PlayerInfo)).rawName = getProperName(username) Then

                                If True Then
                                    Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Someone else logged into this account!"))
                                    currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                End If

                                If True Then
                                    Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Someone else was logged into this account! He was kicked out now."))
                                    peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                End If

                                currentPeer.DisconnectLater(0)
                            End If
                        Next

                        Return 1
                    Else
                        Return -1
                    End If
                Else
                    Return -2
                End If
            End Function

            Public Shared Function playerRegister(ByVal username As String, ByVal password As String, ByVal passwordverify As String, ByVal email As String, ByVal discord As String) As Integer
                username = getProperName(username)
                If Not discord.Contains("#") AndAlso discord.Length <> 0 Then Return -5
                If Not email.Contains("@") AndAlso email.Length <> 0 Then Return -4
                If passwordverify <> password Then Return -3
                If username.Length < 3 Then Return -2
                Dim path As String = "players/" & username & ".json"

                If File.Exists(path) Then
                    Return -1
                End If

                Dim j As JObject = New JObject(New JProperty("username", username), New JProperty("password", hashPassword(password)), New JProperty("email", email), New JProperty("discord", discord), New JProperty("adminLevel", 0))
                File.WriteAllText(path, j.ToString())
                Return 1
            End Function
        End Class

        Public Structure AWorld
            Public info As WorldInfo
            Public id As Integer
        End Structure

        Public Class WorldDB
            Public worlds As WorldInfo() = New WorldInfo() {}

            Public Function get2(ByVal name As String) As AWorld
                If worlds.Length > 200 Then
                    Console.WriteLine("Saving redundant worlds!")
                    saveRedundant()
                    Console.WriteLine("Redundant worlds are saved!")
                End If

                Dim ret As AWorld = New AWorld()
                name = getStrUpper(name)
                If name.Length < 1 Then Throw New Exception("too short name")

                For Each c As Char In name
                    If (c < "A"c OrElse c > "Z"c) AndAlso (c < "0"c OrElse c > "9"c) Then Throw New Exception("bad name")
                Next

                If name = "EXIT" Then
                    Throw New Exception("exit world")
                End If

                For i As Integer = 0 To worlds.Length - 1

                    If worlds(i).name = name Then
                        ret.id = i
                        ret.info = worlds(i)
                        Return ret
                    End If
                Next

                Dim path As String = "worlds/" & name & ".json"

                If File.Exists(path) Then
                    Dim contents As String = File.ReadAllText(path)
                    Dim j As JObject = JObject.Parse(contents)
                    Dim info As WorldInfo = New WorldInfo()
                    info.name = CStr(j("name"))
                    info.width = CInt(j("width"))
                    info.height = CInt(j("height"))
                    info.owner = CStr(j("owner"))
                    info.isPublic = CBool(j("isPublic"))
                    Dim tiles As JArray = CType(j("tiles"), JArray)
                    Dim square As Integer = info.width * info.height
                    info.items = New WorldItem(square - 1) {}

                    For i As Integer = 0 To square - 1
                        info.items(i).foreground = CShort(tiles(i)("fg"))
                        info.items(i).background = CShort(tiles(i)("bg"))
                    Next

                    worlds = worlds.Append(info).ToArray()
                    ret.id = worlds.Length - 1
                    ret.info = info
                    Return ret
                Else
                    Dim info As WorldInfo = generateWorld(name, 100, 60)
                    worlds = worlds.Append(info).ToArray()
                    ret.id = worlds.Length - 1
                    ret.info = info
                    Return ret
                End If
            End Function

            Public Function [get](ByVal name As String) As WorldInfo
                Return get2(name).info
            End Function

            Public Sub flush(ByVal info As WorldInfo)
                Dim path As String = "worlds/" & info.name & ".json"
                Dim tiles As JArray = New JArray()
                Dim square As Integer = info.width * info.height

                For i As Integer = 0 To square - 1
                    Dim tile As JObject = New JObject(New JProperty("fg", info.items(i).foreground), New JProperty("bg", info.items(i).background))
                    tiles.Add(tile)
                Next

                Dim j As JObject = New JObject(New JProperty("name", info.name), New JProperty("width", info.width), New JProperty("height", info.height), New JProperty("owner", info.owner), New JProperty("isPublic", info.isPublic), New JProperty("tiles", tiles))
                File.WriteAllText(path, j.ToString())
            End Sub

            Public Sub flush2(ByVal info As AWorld)
                flush(info.info)
            End Sub

            Public Sub save(ByVal info As AWorld)
                flush2(info)
                Array.Clear(worlds, info.id, 1)
            End Sub

            Public Sub saveAll()
                For i As Integer = 0 To worlds.Length - 1
                    flush(worlds(i))
                Next

                worlds = New WorldInfo() {}
            End Sub

            Public Function getRandomWorlds() As WorldInfo()
                Dim ret As WorldInfo() = New WorldInfo() {}

                For i As Integer = 0 To (If((worlds.Length < 10), worlds.Length, 10)) - 1
                    ret = ret.Append(worlds(i)).ToArray()
                Next

                If worlds.Length > 4 Then
                    Dim rand As Random = New Random()

                    For j As Integer = 0 To 6 - 1
                        Dim isPossible As Boolean = True
                        Dim world As WorldInfo = worlds(rand.[Next](0, worlds.Length - 4))

                        For i As Integer = 0 To ret.Length - 1

                            If world.name = ret(i).name OrElse world.name = "EXIT" Then
                                isPossible = False
                            End If
                        Next

                        If isPossible Then ret = ret.Append(world).ToArray()
                    Next
                End If

                Return ret
            End Function

            Public Sub saveRedundant()
                For i As Integer = 4 To worlds.Length - 1
                    Dim canBeFree As Boolean = True

                    For Each currentPeer As ENetPeer In peers
                        If currentPeer.State <> ENetPeerState.Connected Then Continue For
                        If (TryCast(currentPeer.Data, PlayerInfo)).currentWorld = worlds(i).name Then canBeFree = False
                    Next

                    If canBeFree Then
                        flush(worlds(i))
                        Array.Clear(worlds, i, 1)
                        i -= 1
                    End If
                Next
            End Sub
        End Class

        Public Shared Function getStrUpper(ByVal txt As String) As String
            Dim ret As String = ""

            For Each c As Char In txt
                ret += c.ToString().ToUpper()
            Next

            Return ret
        End Function

        Public Shared Sub saveAllWorlds()
            Console.WriteLine("Saving worlds...")
            worldDB.saveAll()
            Console.WriteLine("Worlds saved!")
        End Sub

        Public Shared Function getPlyersWorld(ByVal peer As ENetPeer) As WorldInfo
            Try
                Return worldDB.get2((TryCast(peer.Data, PlayerInfo)).currentWorld).info
            Catch
                Return Nothing
            End Try
        End Function

        Public Structure PlayerMoving
            Public packetType As Integer
            Public netID As Integer
            Public x As Single
            Public y As Single
            Public characterState As Integer
            Public plantingTree As Integer
            Public XSpeed As Single
            Public YSpeed As Single
            Public punchX As Integer
            Public punchY As Integer
        End Structure

        Public Enum ClothTypes
            HAIR
            SHIRT
            PANTS
            FEET
            FACE
            HAND
            BACK
            MASK
            NECKLACE
            NONE
        End Enum

        Public Enum BlockTypes
            FOREGROUND
            BACKGROUND
            SEED
            PAIN_BLOCK
            BEDROCK
            MAIN_DOOR
            SIGN
            DOOR
            CLOTHING
            FIST
            UNKNOWN
        End Enum

        Public Structure ItemDefinition
            Public id As Integer
            Public name As String
            Public rarity As Integer
            Public breakHits As Integer
            Public growTime As Integer
            Public clothType As ClothTypes
            Public blockType As BlockTypes
            Public description As String
        End Structure

        Public Structure DroppedItem
            Public id As Integer
            Public uid As Integer
            Public count As Integer
        End Structure

        Public Shared Function getItemDef(ByVal id As Integer) As ItemDefinition
            If id < itemDefs.Length AndAlso id > -1 Then Return itemDefs(id)
            Return itemDefs(0)
        End Function

        Public Structure Admin
            Public username As String
            Public password As String
            Public level As Integer
            Public lastSB As Long
        End Structure

        Public Shared Sub craftItemDescriptions()
            If Not File.Exists("Descriptions.txt") Then Return
            Dim contents As String = File.ReadAllText("Descriptions.txt")

            For Each line As String In contents.Split(vbLf.ToCharArray())

                If line.Length > 3 AndAlso line(0) <> "/"c AndAlso line(1) <> "/"c Then
                    Dim ex As String() = explode("|", line)

                    If Convert.ToInt32(ex(0)) + 1 < itemDefs.Length Then
                        itemDefs(Convert.ToInt32(ex(0))).description = ex(1)
                        If (Convert.ToInt32(ex(0)) Mod 2) = 0 Then itemDefs(Convert.ToInt32(ex(0)) + 1).description = "This is a tree."
                    End If
                End If
            Next
        End Sub

        Public Shared Sub buildItemsDatabase()
            Dim current As Integer = -1
            If Not File.Exists("CoreData.txt") Then Return
            Dim contents As String = File.ReadAllText("CoreData.txt")

            For Each line As String In contents.Split(vbLf.ToCharArray())

                If line.Length > 8 AndAlso line(0) <> "/"c AndAlso line(1) <> "/"c Then
                    Dim ex As String() = explode("|", line)
                    Dim def As ItemDefinition = New ItemDefinition()
                    def.id = Convert.ToInt32(ex(0))
                    def.name = ex(1)
                    def.rarity = Convert.ToInt32(ex(2))
                    Dim bt As String = ex(4)

                    If bt = "Foreground_Block" Then
                        def.blockType = BlockTypes.FOREGROUND
                    ElseIf bt = "Seed" Then
                        def.blockType = BlockTypes.SEED
                    ElseIf bt = "Pain_Block" Then
                        def.blockType = BlockTypes.PAIN_BLOCK
                    ElseIf bt = "Main_Door" Then
                        def.blockType = BlockTypes.MAIN_DOOR
                    ElseIf bt = "Bedrock" Then
                        def.blockType = BlockTypes.BEDROCK
                    ElseIf bt = "Door" Then
                        def.blockType = BlockTypes.DOOR
                    ElseIf bt = "Fist" Then
                        def.blockType = BlockTypes.FIST
                    ElseIf bt = "Sign" Then
                        def.blockType = BlockTypes.SIGN
                    ElseIf bt = "Background_Block" Then
                        def.blockType = BlockTypes.BACKGROUND
                    Else
                        def.blockType = BlockTypes.UNKNOWN
                    End If

                    def.breakHits = Convert.ToInt32(ex(7))
                    def.growTime = Convert.ToInt32(ex(8))
                    Dim cl As String = ex(9)

                    If cl = "None" Then
                        def.clothType = ClothTypes.NONE
                    ElseIf cl = "Hat" Then
                        def.clothType = ClothTypes.HAIR
                    ElseIf cl = "Shirt" Then
                        def.clothType = ClothTypes.SHIRT
                    ElseIf cl = "Pants" Then
                        def.clothType = ClothTypes.PANTS
                    ElseIf cl = "Feet" Then
                        def.clothType = ClothTypes.FEET
                    ElseIf cl = "Face" Then
                        def.clothType = ClothTypes.FACE
                    ElseIf cl = "Hand" Then
                        def.clothType = ClothTypes.HAND
                    ElseIf cl = "Back" Then
                        def.clothType = ClothTypes.BACK
                    ElseIf cl = "Hair" Then
                        def.clothType = ClothTypes.MASK
                    ElseIf cl = "Chest" Then
                        def.clothType = ClothTypes.NECKLACE
                    Else
                        def.clothType = ClothTypes.NONE
                    End If

                    If System.Threading.Interlocked.Increment(current) <> def.id Then
                        Console.WriteLine("Critical error! Unordered database at item " & current & "/" & def.id)
                    End If

                    itemDefs = itemDefs.Append(def).ToArray()
                End If
            Next

            craftItemDescriptions()
        End Sub

        Public Sub addAdmin(ByVal username As String, ByVal password As String, ByVal level As Integer)
            Dim admin As Admin = New Admin()
            admin.username = username
            admin.password = password
            admin.level = level
            admins = admins.Append(admin).ToArray()
        End Sub

        Public Shared Function getAdminLevel(ByVal username As String, ByVal password As String) As Integer
            For i As Integer = 0 To admins.Length - 1
                Dim admin As Admin = admins(i)

                If admin.username = username AndAlso admin.password = password Then
                    Return admin.level
                End If
            Next

            Return 0
        End Function

        Public Shared Function canSB(ByVal username As String, ByVal password As String) As Boolean
            For i As Integer = 0 To admins.Length - 1
                Dim admin As Admin = admins(i)

                If admin.username = username AndAlso admin.password = password AndAlso admin.level > 1 Then
                    Dim time As Long = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

                    If admin.lastSB + 900000 < time OrElse admin.level = 999 Then
                        admins(i).lastSB = time
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        Public Function canClear(ByVal username As String, ByVal password As String) As Boolean
            For i As Integer = 0 To admins.Length - 1
                Dim admin As Admin = admins(i)

                If admin.username = username AndAlso admin.password = password Then
                    Return admin.level > 0
                End If
            Next

            Return False
        End Function

        Public Shared Function isSuperAdmin(ByVal username As String, ByVal password As String) As Boolean
            For i As Integer = 0 To admins.Length - 1
                Dim admin As Admin = admins(i)

                If admin.username = username AndAlso admin.password = password AndAlso admin.level = 999 Then
                    Return True
                End If
            Next

            Return False
        End Function

        Public Shared Function isHere(ByVal peer As ENetPeer, ByVal peer2 As ENetPeer) As Boolean
            Return ((TryCast(peer.Data, PlayerInfo)).currentWorld = (TryCast(peer2.Data, PlayerInfo)).currentWorld)
        End Function

        Public Shared Sub sendInventory(ByVal peer As ENetPeer, ByVal inventory As PlayerInventory)
            Dim asdf2 As String = "0400000009A7379237BB2509E8E0EC04F8720B050000000000000000FBBB0000010000007D920100FDFDFDFD04000000040000000000000000000000000000000000"
            Dim inventoryLen As Integer = inventory.items.Length
            Dim packetLen As Integer = (asdf2.Length / 2) + (inventoryLen * 4) + 4
            Dim data2 As Byte() = New Byte(packetLen - 1) {}

            For i As Integer = 0 To asdf2.Length - 1 Step 2
                Dim x As Byte = ch2n(asdf2(i))
                                ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 				x = (byte)(x << 4);

''' 
                x += ch2n(asdf2(i + 1))
                data2(i / 2) = x
            Next

            Dim endianInvVal As Byte() = BitConverter.GetBytes(inventoryLen)
            Array.Reverse(endianInvVal)
            Array.Copy(endianInvVal, 0, data2, asdf2.Length / 2 - 4, 4)
            endianInvVal = BitConverter.GetBytes(inventory.inventorySize)
            Array.Reverse(endianInvVal)
            Array.Copy(endianInvVal, 0, data2, asdf2.Length / 2 - 8, 4)
            Dim val As Integer = 0

            For i As Integer = 0 To inventoryLen - 1
                val = 0
                val = val Or inventory.items(i).itemID
                                ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 				val |= inventory.items[i].itemCount << 16;

''' 
                val = val And &H00FFFFFF
                                ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 				val |= 0x00 << 24;

''' 
                Dim value As Byte() = BitConverter.GetBytes(val)
                Array.Copy(value, 0, data2, asdf2.Length / 2 + (i * 4), 4)
            Next

            peer.Send(data2, 0, ENetPacketFlags.Reliable)
        End Sub

        Public Shared Function packPlayerMoving(ByVal dataStruct As PlayerMoving) As Byte()
            Dim data As Byte() = New Byte(55) {}

            For i As Integer = 0 To 56 - 1
                data(i) = 0
            Next

            Array.Copy(BitConverter.GetBytes(dataStruct.packetType), 0, data, 0, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.netID), 0, data, 4, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.characterState), 0, data, 12, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.plantingTree), 0, data, 20, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.x), 0, data, 24, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.y), 0, data, 28, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.XSpeed), 0, data, 32, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.YSpeed), 0, data, 36, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.punchX), 0, data, 44, 4)
            Array.Copy(BitConverter.GetBytes(dataStruct.punchY), 0, data, 48, 4)
            Return data
        End Function

        Public Shared Function unpackPlayerMoving(ByVal data As Byte()) As PlayerMoving
            Dim dataStruct As PlayerMoving = New PlayerMoving()
            dataStruct.packetType = BitConverter.ToInt32(data, 0)
            dataStruct.netID = BitConverter.ToInt32(data, 4)
            dataStruct.characterState = BitConverter.ToInt32(data, 12)
            dataStruct.plantingTree = BitConverter.ToInt32(data, 20)
            dataStruct.x = BitConverter.ToInt32(data, 24)
            dataStruct.y = BitConverter.ToInt32(data, 28)
            dataStruct.XSpeed = BitConverter.ToInt32(data, 32)
            dataStruct.YSpeed = BitConverter.ToInt32(data, 36)
            dataStruct.punchX = BitConverter.ToInt32(data, 44)
            dataStruct.punchY = BitConverter.ToInt32(data, 48)
            Return dataStruct
        End Function

        Public Sub SendPacket(ByVal a1 As Integer, ByVal a2 As String, ByVal enetPeer As ENetPeer)
            If enetPeer IsNot Nothing Then
                Dim v3 As Byte() = New Byte(a2.Length + 5 - 1) {}
                Array.Copy(BitConverter.GetBytes(a1), 0, v3, 0, 4)
                Array.Copy(Encoding.ASCII.GetBytes(a2), 0, v3, 4, a2.Length)
                enetPeer.Send(v3, 0, ENetPacketFlags.Reliable)
            End If
        End Sub

        Public Shared Sub SendPacketRaw(ByVal a1 As Integer, ByVal packetData As Byte(), ByVal packetDataSize As Long, ByVal a4 As Integer, ByVal peer As ENetPeer, ByVal packetFlag As Integer)
            If peer IsNot Nothing Then

                If a1 = 4 AndAlso (packetData(12) And 8) = 1 Then
                    Dim p As Byte() = New Byte(packetDataSize + packetData(13) - 1) {}
                    Array.Copy(BitConverter.GetBytes(4), 0, p, 0, 4)
                    Array.Copy(packetData, 0, p, 4, packetDataSize)
                    Array.Copy(BitConverter.GetBytes(a4), 0, p, 4 + packetDataSize, 4)
                    peer.Send(p, 0, ENetPacketFlags.Reliable)
                Else
                    Dim p As Byte() = New Byte(packetDataSize + 5 - 1) {}
                    Array.Copy(BitConverter.GetBytes(a1), 0, p, 0, 4)
                    Array.Copy(packetData, 0, p, 4, packetDataSize)
                    peer.Send(p, 0, ENetPacketFlags.Reliable)
                End If
            End If
        End Sub

        Public Shared Sub onPeerConnect(ByVal peer As ENetPeer)
            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If peer <> currentPeer Then

                    If isHere(peer, currentPeer) Then
                        Dim netIdS As String = (TryCast(currentPeer.Data, PlayerInfo)).netID.ToString()
                        Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnSpawn"), "spawn|avatar" & vbLf & "netID|" & netIdS & vbLf & "userID|" & netIdS & vbLf & "colrect|0|0|20|30" & vbLf & "posXY|" & (TryCast(currentPeer.Data, PlayerInfo)).x & "|" + (TryCast(currentPeer.Data, PlayerInfo)).y & vbLf & "name|``" & (TryCast(currentPeer.Data, PlayerInfo)).displayName & "``" & vbLf & "country|" & (TryCast(currentPeer.Data, PlayerInfo)).country & vbLf & "invis|0" & vbLf & "mstate|0" & vbLf & "smstate|0" & vbLf))
                        peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                        Dim netIdS2 As String = (TryCast(peer.Data, PlayerInfo)).netID.ToString()
                        Dim p2 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnSpawn"), "spawn|avatar" & vbLf & "netID|" & netIdS2 & vbLf & "userID|" & netIdS2 & vbLf & "colrect|0|0|20|30" & vbLf & "posXY|" & (TryCast(currentPeer.Data, PlayerInfo)).x & "|" + (TryCast(currentPeer.Data, PlayerInfo)).y & vbLf & "name|``" & (TryCast(currentPeer.Data, PlayerInfo)).displayName & "``" & vbLf & "country|" & (TryCast(currentPeer.Data, PlayerInfo)).country & vbLf & "invis|0" & vbLf & "mstate|0" & vbLf & "smstate|0" & vbLf))
                        peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                    End If
                End If
            Next
        End Sub

        Public Shared Sub updateAllClothes(ByVal peer As ENetPeer)
            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Dim p3 As GamePacket = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (TryCast(peer.Data, PlayerInfo)).cloth_hair, (TryCast(peer.Data, PlayerInfo)).cloth_shirt, (TryCast(peer.Data, PlayerInfo)).cloth_pants), (TryCast(peer.Data, PlayerInfo)).cloth_feet, (TryCast(peer.Data, PlayerInfo)).cloth_face, (TryCast(peer.Data, PlayerInfo)).cloth_hand), (TryCast(peer.Data, PlayerInfo)).cloth_back, (TryCast(peer.Data, PlayerInfo)).cloth_mask, (TryCast(peer.Data, PlayerInfo)).cloth_necklace), CInt((TryCast(peer.Data, PlayerInfo)).skinColor)), 0.0F, 0.0F, 0.0F))
                    Array.Copy(BitConverter.GetBytes((TryCast(peer.Data, PlayerInfo)).netID), 0, p3.data, 8, 4)
                    peer.Send(p3.data, 0, ENetPacketFlags.Reliable)
                    Dim p4 As GamePacket = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (TryCast(currentPeer.Data, PlayerInfo)).cloth_hair, (TryCast(currentPeer.Data, PlayerInfo)).cloth_shirt, (TryCast(currentPeer.Data, PlayerInfo)).cloth_pants), (TryCast(currentPeer.Data, PlayerInfo)).cloth_feet, (TryCast(currentPeer.Data, PlayerInfo)).cloth_face, (TryCast(currentPeer.Data, PlayerInfo)).cloth_hand), (TryCast(currentPeer.Data, PlayerInfo)).cloth_back, (TryCast(currentPeer.Data, PlayerInfo)).cloth_mask, (TryCast(currentPeer.Data, PlayerInfo)).cloth_necklace), CInt((TryCast(currentPeer.Data, PlayerInfo)).skinColor)), 0.0F, 0.0F, 0.0F))
                    Array.Copy(BitConverter.GetBytes((TryCast(currentPeer.Data, PlayerInfo)).netID), 0, p3.data, 8, 4)
                    peer.Send(p4.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendClothes(ByVal peer As ENetPeer)
            Dim p3 As GamePacket = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (TryCast(peer.Data, PlayerInfo)).cloth_hair, (TryCast(peer.Data, PlayerInfo)).cloth_shirt, (TryCast(peer.Data, PlayerInfo)).cloth_pants), (TryCast(peer.Data, PlayerInfo)).cloth_feet, (TryCast(peer.Data, PlayerInfo)).cloth_face, (TryCast(peer.Data, PlayerInfo)).cloth_hand), (TryCast(peer.Data, PlayerInfo)).cloth_back, (TryCast(peer.Data, PlayerInfo)).cloth_mask, (TryCast(peer.Data, PlayerInfo)).cloth_necklace), CInt((TryCast(peer.Data, PlayerInfo)).skinColor)), 0.0F, 0.0F, 0.0F))

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Array.Copy(BitConverter.GetBytes((TryCast(peer.Data, PlayerInfo)).netID), 0, p3.data, 8, 4)
                    peer.Send(p3.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendPData(ByVal peer As ENetPeer, ByVal data As PlayerMoving)
            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If peer <> currentPeer Then

                    If isHere(peer, currentPeer) Then
                        data.netID = (TryCast(peer.Data, PlayerInfo)).netID
                        SendPacketRaw(4, packPlayerMoving(data), 56, 0, currentPeer, 0)
                    End If
                End If
            Next
        End Sub

        Public Shared Function getPlayersCountInWorld(ByVal name As String) As Integer
            Dim count As Integer = 0

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For
                If (TryCast(currentPeer.Data, PlayerInfo)).currentWorld = name Then count += 1
            Next

            Return count
        End Function

        Public Shared Sub sendRoulete(ByVal peer As ENetPeer, ByVal x As Integer, ByVal y As Integer)
            Dim rand As Random = New Random()
            Dim val As Integer = rand.[Next](0, 37)

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Dim p2 As GamePacket = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), (TryCast(peer.Data, PlayerInfo)).netID), "`w[" & (TryCast(peer.Data, PlayerInfo)).displayName & " `wspun the wheel and got `6" + val & "`w!]"), 0))
                    currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendNothingHappened(ByVal peer As ENetPeer, ByVal x As Integer, ByVal y As Integer)
            Dim data As PlayerMoving = New PlayerMoving()
            data.netID = (TryCast(peer.Data, PlayerInfo)).netID
            data.packetType = &H8
            data.plantingTree = 0
            data.netID = -1
            data.x = x
            data.y = y
            data.punchX = x
            data.punchY = y
            SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0)
        End Sub

        Public Shared Sub sendTileUpdate(ByVal x As Integer, ByVal y As Integer, ByVal tile As Integer, ByVal causedBy As Integer, ByVal peer As ENetPeer)
            Dim data As PlayerMoving
            data.packetType = &H3
            data.characterState = &H0
            data.x = x
            data.y = y
            data.punchX = x
            data.punchY = y
            data.XSpeed = 0
            data.YSpeed = 0
            data.netID = causedBy
            data.plantingTree = tile
            Dim world As WorldInfo = getPlyersWorld(peer)
            If world Is Nothing Then Return
            If x < 0 OrElse y < 0 OrElse x > world.width OrElse y > world.height Then Return
            sendNothingHappened(peer, x, y)

            If Not isSuperAdmin((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) Then
                If world.items(x + (y * world.width)).foreground = 6 OrElse world.items(x + (y * world.width)).foreground = 8 OrElse world.items(x + (y * world.width)).foreground = 3760 Then Return
                If tile = 6 OrElse tile = 8 OrElse tile = 3760 Then Return
            End If

            If world.name = "ADMIN" AndAlso getAdminLevel((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) = 0 Then
                If world.items(x + (y * world.width)).foreground = 758 Then sendRoulete(peer, x, y)
                Return
            End If

            If world.name <> "ADMIN" Then

                If world.owner <> "" Then

                    If (TryCast(peer.Data, PlayerInfo)).rawName = world.owner Then

                        If tile = 32 Then
                            Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o" & vbLf & vbLf & "add_label_with_icon|big|`wShould this world be publicly breakable?``|left|242|" & vbLf & vbLf & "add_spacer|small|" & vbLf & "add_button_with_icon|worldPublic|Public|noflags|2408||" & vbLf & "add_button_with_icon|worldPrivate|Private|noflags|202||" & vbLf & "add_spacer|small|" & vbLf & "add_quick_exit|" & vbLf & "add_button|chc0|Close|noflags|0|0|" & vbLf & "nend_dialog|gazette||OK|"))
                            peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                        End If
                    ElseIf world.isPublic Then

                        If world.items(x + (y * world.width)).foreground = 242 Then
                            Return
                        End If
                    Else
                        Return
                    End If

                    If tile = 242 Then
                        Return
                    End If
                End If
            End If

            If tile = 32 Then
                Return
            End If

            If tile = 822 Then
                world.items(x + (y * world.width)).water = Not world.items(x + (y * world.width)).water
                Return
            End If

            If tile = 3062 Then
                world.items(x + (y * world.width)).fire = Not world.items(x + (y * world.width)).fire
                Return
            End If

            If tile = 1866 Then
                world.items(x + (y * world.width)).glue = Not world.items(x + (y * world.width)).glue
                Return
            End If

            Dim def As ItemDefinition

            Try
                def = getItemDef(tile)
                If def.clothType <> ClothTypes.NONE Then Return
            Catch
                def.breakHits = 4
                def.blockType = BlockTypes.UNKNOWN
            End Try

            If tile = 544 OrElse tile = 546 OrElse tile = 4520 OrElse tile = 382 OrElse tile = 3116 OrElse tile = 4520 OrElse tile = 1792 OrElse tile = 5666 OrElse tile = 2994 OrElse tile = 4368 Then Return
            If tile = 5708 OrElse tile = 5709 OrElse tile = 5780 OrElse tile = 5781 OrElse tile = 5782 OrElse tile = 5783 OrElse tile = 5784 OrElse tile = 5785 OrElse tile = 5710 OrElse tile = 5711 OrElse tile = 5786 OrElse tile = 5787 OrElse tile = 5788 OrElse tile = 5789 OrElse tile = 5790 OrElse tile = 5791 OrElse tile = 6146 OrElse tile = 6147 OrElse tile = 6148 OrElse tile = 6149 OrElse tile = 6150 OrElse tile = 6151 OrElse tile = 6152 OrElse tile = 6153 OrElse tile = 5670 OrElse tile = 5671 OrElse tile = 5798 OrElse tile = 5799 OrElse tile = 5800 OrElse tile = 5801 OrElse tile = 5802 OrElse tile = 5803 OrElse tile = 5668 OrElse tile = 5669 OrElse tile = 5792 OrElse tile = 5793 OrElse tile = 5794 OrElse tile = 5795 OrElse tile = 5796 OrElse tile = 5797 OrElse tile = 544 OrElse tile = 546 OrElse tile = 4520 OrElse tile = 382 OrElse tile = 3116 OrElse tile = 1792 OrElse tile = 5666 OrElse tile = 2994 OrElse tile = 4368 Then Return
            If tile = 1902 OrElse tile = 1508 OrElse tile = 428 Then Return
            If tile = 410 OrElse tile = 1770 OrElse tile = 4720 OrElse tile = 4882 OrElse tile = 6392 OrElse tile = 3212 OrElse tile = 1832 OrElse tile = 4742 OrElse tile = 3496 OrElse tile = 3270 OrElse tile = 4722 Then Return
            If tile >= 7068 Then Return

            If tile = 0 OrElse tile = 18 Then
                data.packetType = &H8
                data.plantingTree = 4
                Dim time As Long = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

                If time - world.items(x + (y * world.width)).breakTime >= 4000 Then
                    world.items(x + (y * world.width)).breakTime = time
                    world.items(x + (y * world.width)).breakLevel = 4
                    If world.items(x + (y * world.width)).foreground = 758 Then sendRoulete(peer, x, y)
                ElseIf y < world.height AndAlso world.items(x + (y * world.width)).breakLevel + 4 >= def.breakHits * 4 Then
                    data.packetType = &H3
                    data.netID = -1
                    data.plantingTree = 0
                    world.items(x + (y * world.width)).breakLevel = 0

                    If world.items(x + (y * world.width)).foreground <> 0 Then

                        If world.items(x + (y * world.width)).foreground = 242 Then
                            world.owner = ""
                            world.isPublic = False
                        End If

                        world.items(x + (y * world.width)).foreground = 0
                    Else
                        world.items(x + (y * world.width)).background = 0
                    End If
                ElseIf y < world.height Then
                    world.items(x + (y * world.width)).breakTime = time
                    world.items(x + (y * world.width)).breakLevel += 4
                    If world.items(x + (y * world.width)).foreground = 758 Then sendRoulete(peer, x, y)
                End If
            Else

                For i As Integer = 0 To (TryCast(peer.Data, PlayerInfo)).inventory.items.Length - 1

                    If (TryCast(peer.Data, PlayerInfo)).inventory.items(i).itemID = tile Then

                        If CUInt((TryCast(peer.Data, PlayerInfo)).inventory.items(i).itemCount) > 1 Then
                            (TryCast(peer.Data, PlayerInfo)).inventory.items(i).itemCount -= 1
                        Else
                            Array.Clear((TryCast(peer.Data, PlayerInfo)).inventory.items, i, 1)
                        End If
                    End If
                Next

                If def.blockType = BlockTypes.BACKGROUND Then
                    world.items(x + (y * world.width)).background = CShort(tile)
                Else
                    world.items(x + (y * world.width)).foreground = CShort(tile)

                    If tile = 242 Then
                        world.owner = (TryCast(peer.Data, PlayerInfo)).rawName
                        world.isPublic = False

                        For Each currentPeer As ENetPeer In peers
                            If currentPeer.State <> ENetPeerState.Connected Then Continue For

                            If isHere(peer, currentPeer) Then
                                Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`3[`w" & world.name & " `ohas been World Locked by `2" & (TryCast(peer.Data, PlayerInfo)).displayName & "`3]"))
                                currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable)
                            End If
                        Next
                    End If
                End If

                world.items(x + (y * world.width)).breakLevel = 0
            End If

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For
                If isHere(peer, currentPeer) Then SendPacketRaw(4, packPlayerMoving(data), 56, 0, currentPeer, 0)
            Next
        End Sub

        Public Shared Sub sendPlayerLeave(ByVal peer As ENetPeer, ByVal player As PlayerInfo)
            Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnRemove"), "netID|" & player.netID & vbLf))
            Dim p2 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`5<`w" & player.displayName & "`` left, `w" & getPlayersCountInWorld(player.currentWorld) & "`` others here>``"))

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then

                    If True Then
                        peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                    End If

                    If True Then
                        currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                    End If
                End If
            Next
        End Sub

        Public Shared Sub sendChatMessage(ByVal peer As ENetPeer, ByVal netID As Integer, ByVal message As String)
            If message.Length = 0 Then Return
            Dim name As String = ""

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For
                If (TryCast(currentPeer.Data, PlayerInfo)).netID = netID Then name = (TryCast(currentPeer.Data, PlayerInfo)).displayName
            Next

            Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`o<`w" & name & "`o> " & message))
            Dim p2 As GamePacket = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), netID), message), 0))

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable)
                    currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendWho(ByVal peer As ENetPeer)
            Dim name As String = ""

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Dim p2 As GamePacket = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), (TryCast(currentPeer.Data, PlayerInfo)).netID), (TryCast(currentPeer.Data, PlayerInfo)).displayName), 1))
                    peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendWorld(ByVal peer As ENetPeer, ByVal worldInfo As WorldInfo)
            Console.WriteLine("Entering a world...")
            (TryCast(peer.Data, PlayerInfo)).joinClothesUpdated = False
            Dim asdf As String = "0400000004A7379237BB2509E8E0EC04F8720B050000000000000000FBBB0000010000007D920100FDFDFDFD04000000040000000000000000000000070000000000"
            Dim worldName As String = worldInfo.name
            Dim xSize As Integer = worldInfo.width
            Dim ySize As Integer = worldInfo.height
            Dim square As Integer = xSize * ySize
            Dim nameLen As Int16 = CShort(worldName.Length)
            Dim payloadLen As Integer = asdf.Length / 2
            Dim dataLen As Integer = payloadLen + 2 + nameLen + 12 + (square * 8) + 4
            Dim allocMem As Integer = payloadLen + 2 + nameLen + 12 + (square * 8) + 4 + 16000
            Dim data As Byte() = New Byte(allocMem - 1) {}

            For i As Integer = 0 To asdf.Length - 1 Step 2
                Dim x As Byte = ch2n(asdf(i))
                                ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 				x = (byte) (x << 4);

''' 
                x += ch2n(asdf(i + 1))
                data(i / 2) = x
            Next

            Dim zero As Integer = 0
            Dim item As Int16 = 0
            Dim smth As Integer = 0

            For i As Integer = 0 To square * 8 - 1 Step 4
                Array.Copy(BitConverter.GetBytes(zero), 0, data, payloadLen + i + 14 + nameLen, 4)
            Next

            For i As Integer = 0 To square * 8 - 1 Step 8
                Array.Copy(BitConverter.GetBytes(item), 0, data, payloadLen + i + 14 + nameLen, 2)
            Next

            Array.Copy(BitConverter.GetBytes(nameLen), 0, data, payloadLen, 2)
            Array.Copy(Encoding.ASCII.GetBytes(worldName), 0, data, payloadLen + 2, nameLen)
            Array.Copy(BitConverter.GetBytes(xSize), 0, data, payloadLen + 2 + nameLen, 4)
            Array.Copy(BitConverter.GetBytes(ySize), 0, data, payloadLen + 6 + nameLen, 4)
            Array.Copy(BitConverter.GetBytes(square), 0, data, payloadLen + 10 + nameLen, 4)
            Dim blockPtr As Integer = payloadLen + 14 + nameLen

            For i As Integer = 0 To square - 1

                If (worldInfo.items(i).foreground = 0) OrElse (worldInfo.items(i).foreground = 2) OrElse (worldInfo.items(i).foreground = 8) OrElse (worldInfo.items(i).foreground = 100) Then
                    Array.Copy(BitConverter.GetBytes(worldInfo.items(i).foreground), 0, data, blockPtr, 2)
                    Dim type As Long = &H00000000
                    If worldInfo.items(i).water Then type = type Or &H04000000
                    If worldInfo.items(i).glue Then type = type Or &H08000000
                    If worldInfo.items(i).fire Then type = type Or &H10000000
                    If worldInfo.items(i).red Then type = type Or &H20000000
                    If worldInfo.items(i).green Then type = type Or &H40000000
                    If worldInfo.items(i).blue Then type = type Or &H80000000
                    Array.Copy(BitConverter.GetBytes(type), 0, data, blockPtr + 4, 4)
                Else
                    Array.Copy(BitConverter.GetBytes(zero), 0, data, blockPtr, 2)
                End If

                Array.Copy(BitConverter.GetBytes(worldInfo.items(i).background), 0, data, blockPtr + 2, 2)
                blockPtr += 8
            Next

            Array.Copy(BitConverter.GetBytes(smth), 0, data, dataLen - 4, 4)
            peer.Send(data, 0, ENetPacketFlags.Reliable)

            For i As Integer = 0 To square - 1

                If (worldInfo.items(i).foreground = 0) OrElse (worldInfo.items(i).foreground = 2) OrElse (worldInfo.items(i).foreground = 8) OrElse (worldInfo.items(i).foreground = 100) Then
                Else
                    Dim data1 As PlayerMoving
                    data1.packetType = &H3
                    data1.characterState = &H0
                    data1.x = i Mod worldInfo.width
                    data1.y = i / worldInfo.height
                    data1.punchX = i Mod worldInfo.width
                    data1.punchY = i / worldInfo.width
                    data1.XSpeed = 0
                    data1.YSpeed = 0
                    data1.netID = -1
                    data1.plantingTree = worldInfo.items(i).foreground
                    SendPacketRaw(4, packPlayerMoving(data1), 56, 0, peer, 0)
                End If
            Next

            (TryCast(peer.Data, PlayerInfo)).currentWorld = worldInfo.name
        End Sub

        Public Shared Sub sendAction(ByVal peer As ENetPeer, ByVal netID As Integer, ByVal action As String)
            Dim name As String = ""
            Dim p2 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnAction"), action))

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Array.Copy(BitConverter.GetBytes(netID), 0, p2.data, 8, 4)
                    currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                End If
            Next
        End Sub

        Public Shared Sub sendDrop(ByVal peer As ENetPeer, ByVal netID As Integer, ByVal x As Integer, ByVal y As Integer, ByVal item As Integer, ByVal count As Integer, ByVal specialEffect As Byte)
            If item >= 7068 Then Return
            If item < 0 Then Return
            Dim name As String = ""

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Dim data As PlayerMoving = New PlayerMoving()
                    data.packetType = 14
                    data.x = x
                    data.y = y
                    data.netID = netID
                    data.plantingTree = item
                    Dim val As Single = count
                    Dim val2 As Byte = specialEffect
                    Dim raw As Byte() = packPlayerMoving(data)
                    Array.Copy(BitConverter.GetBytes(val), 0, raw, 16, 4)
                    Array.Copy(BitConverter.GetBytes(val2), 0, raw, 1, 1)
                    SendPacketRaw(4, raw, 56, 0, currentPeer, 0)
                End If
            Next
        End Sub

        Public Shared Sub sendState(ByVal peer As ENetPeer)
            Dim info As PlayerInfo = TryCast(peer.Data, PlayerInfo)
            Dim netID As Integer = info.netID
            Dim state As Integer = getState(info)

            For Each currentPeer As ENetPeer In peers
                If currentPeer.State <> ENetPeerState.Connected Then Continue For

                If isHere(peer, currentPeer) Then
                    Dim data As PlayerMoving
                    data.packetType = &H14
                    data.characterState = 0
                    data.x = 1000
                    data.y = 100
                    data.punchX = 0
                    data.punchY = 0
                    data.XSpeed = 300
                    data.YSpeed = 600
                    data.netID = netID
                    data.plantingTree = state
                    Dim raw As Byte() = packPlayerMoving(data)
                    Dim var As Integer = &H808000
                    Array.Copy(BitConverter.GetBytes(var), 0, raw, 1, 3)
                    SendPacketRaw(4, raw, 56, 0, currentPeer, 0)
                End If
            Next
        End Sub

        Public Shared Sub sendWorldOffers(ByVal peer As ENetPeer)
            If Not (TryCast(peer.Data, PlayerInfo)).isIn Then Return
            Dim worldz As WorldInfo() = worldDB.getRandomWorlds()
            Dim worldOffers As String = "default|"

            If worldz.Length > 0 Then
                worldOffers += worldz(0).name
            End If

            worldOffers += vbLf & "add_button|Showing: `wWorlds``|_catselect_|0.6|3529161471|" & vbLf

            For i As Integer = 0 To worldz.Length - 1
                worldOffers += "add_floater|" & worldz(i).name & "|" & getPlayersCountInWorld(worldz(i).name) & "|0.55|3529161471" & vbLf
            Next

            Dim p3 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnRequestWorldSelectMenu"), worldOffers))
            peer.Send(p3.data, 0, ENetPacketFlags.Reliable)
        End Sub

        Public Shared Sub HandlerRoutine(ByVal sender As Object, ByVal e As ConsoleCancelEventArgs)
            saveAllWorlds()
            Environment.[Exit](0)
        End Sub

        Private Shared Sub Main(ByVal args As String())
            Console.WriteLine("Growtopia private server (c) willi12yao")
            ManagedENet.Startup()
            AddHandler Console.CancelKeyPress, AddressOf HandlerRoutine

            If File.Exists("items.dat") Then
                Dim itemsData As Byte() = File.ReadAllBytes("items.dat")
                itemsDatSize = itemsData.Length
                itemsDat = New Byte(60 + itemsDatSize - 1) {}
                Dim asdf As String = "0400000010000000FFFFFFFF000000000800000000000000000000000000000000000000000000000000000000000000000000000000000000000000"

                For i As Integer = 0 To asdf.Length - 1 Step 2
                    Dim x As Byte = ch2n(asdf(i))
                                        ''' Cannot convert ExpressionStatementSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: op
''' Actual value was LeftShiftExpression.
'''    at ICSharpCode.CodeConverter.Util.VBUtil.GetExpressionOperatorTokenKind(SyntaxKind op)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitBinaryExpression(BinaryExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitCastExpression(CastExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.ConvertSingleExpression(ExpressionSyntax node)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax.Accept[TResult](CSharpSyntaxVisitor`1 visitor)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input: 
''' 					x = (byte) (x << 4);

''' 
                    x += ch2n(asdf(i + 1))
                    itemsDat(i / 2) = x
                    If asdf.Length > 60 * 2 Then Throw New Exception("Error")
                Next

                Array.Copy(BitConverter.GetBytes(itemsDatSize), 0, itemsDat, 56, 4)
                Console.WriteLine("Updating item data success!")
            Else
                Console.WriteLine("Updating item data failed")
            End If

            worldDB = New WorldDB()
            worldDB.[get]("TEST")
            worldDB.[get]("MAIN")
            worldDB.[get]("NEW")
            worldDB.[get]("ADMIN")
            Dim address As IPEndPoint = New IPEndPoint(IPAddress.Any, 17091)
            server = New ENetHost(address, 1024, 10)
            server.ChecksumWithCRC32()
            server.CompressWithRangeCoder()
            Console.WriteLine("Building items database...")
            buildItemsDatabase()
            Console.WriteLine("Database is built!")
            server.OnConnect += Function(ByVal sender As Object, ByVal eve As ENetConnectEventArgs)

                                    If True Then
                                        Dim peer As ENetPeer = eve.Peer
                                        Console.WriteLine("A new client connected.")
                                        Dim count As Integer = 0

                                        For Each currentPeer As ENetPeer In peers
                                            If currentPeer.State <> ENetPeerState.Connected Then Continue For
                                            If currentPeer.RemoteEndPoint.Equals(peer.RemoteEndPoint) Then count += 1
                                        Next

                                        peer.Data = New PlayerInfo()

                                        If count > 3 Then
                                            Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rToo many accounts are logged on from this IP. Log off one account before playing please.``"))
                                            peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                            peer.DisconnectLater(0)
                                        Else
                                            sendData(peer, 1, BitConverter.GetBytes(0), 0)
                                            peers.Add(peer)
                                        End If
                                    End If

                                    eve.Peer.OnReceive += Function(ByVal send As Object, ByVal ev As ENetPacket)
                                                              Dim pak As Byte() = ev.GetPayloadCopy()
                                                              Dim peer As ENetPeer = TryCast(send, ENetPeer)

                                                              If (TryCast(peer.Data, PlayerInfo)).isUpdating Then
                                                                  Console.WriteLine("packet drop")
                                                                  Return
                                                              End If

                                                              Dim messageType As Integer = pak(0)
                                                              Dim world As WorldInfo = getPlyersWorld(peer)

                                                              Select Case messageType
                                                                  Case 2
                                                                      Dim cch As String = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray())

                                                                      If cch.IndexOf("action|respawn") = 0 Then

                                                                          If True Then
                                                                              Dim x As Integer = 3040
                                                                              Dim y As Integer = 736
                                                                              If world Is Nothing Then Return

                                                                              For i As Integer = 0 To world.width * world.height - 1

                                                                                  If world.items(i).foreground = 6 Then
                                                                                      x = (i Mod world.width) * 32
                                                                                      y = (i / world.width) * 32
                                                                                  End If
                                                                              Next

                                                                              Dim data As PlayerMoving
                                                                              data.packetType = &H0
                                                                              data.characterState = &H924
                                                                              data.x = x
                                                                              data.y = y
                                                                              data.punchX = -1
                                                                              data.punchY = -1
                                                                              data.XSpeed = 0
                                                                              data.YSpeed = 0
                                                                              data.netID = (TryCast(peer.Data, PlayerInfo)).netID
                                                                              data.plantingTree = &H0
                                                                              SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0)
                                                                          End If

                                                                          If True Then
                                                                              Dim x As Integer = 3040
                                                                              Dim y As Integer = 736

                                                                              For i As Integer = 0 To world.width * world.height - 1

                                                                                  If world.items(i).foreground = 6 Then
                                                                                      x = (i Mod world.width) * 32
                                                                                      y = (i / world.width) * 32
                                                                                  End If
                                                                              Next

                                                                              Dim p2 As GamePacket = packetEnd(appendFloat(appendString(createPacket(), "OnSetPos"), x, y))
                                                                              Array.Copy(BitConverter.GetBytes((TryCast(peer.Data, PlayerInfo)).netID), 0, p2.data, 8, 4)
                                                                              peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                          End If

                                                                          If True Then
                                                                              Dim x As Integer = 3040
                                                                              Dim y As Integer = 736

                                                                              For i As Integer = 0 To world.width * world.height - 1

                                                                                  If world.items(i).foreground = 6 Then
                                                                                      x = (i Mod world.width) * 32
                                                                                      y = (i / world.width) * 32
                                                                                  End If
                                                                              Next

                                                                              Dim p2 As GamePacket = packetEnd(appendIntx(appendString(createPacket(), "OnSetFreezeState"), 0))
                                                                              Array.Copy(BitConverter.GetBytes((TryCast(peer.Data, PlayerInfo)).netID), 0, p2.data, 8, 4)
                                                                              peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                          End If

                                                                          Console.WriteLine("Respawning")
                                                                      End If

                                                                      If cch.IndexOf("action|growid") = 0 Then
                                                                          Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o" & vbLf & vbLf & "add_label_with_icon|big|`wGet a GrowID``|left|206|" & vbLf & vbLf & "add_spacer|small|" & vbLf & "add_textbox|A `wGrowID `wmeans `oyou can use a name and password to logon from any device.|" & vbLf & "add_spacer|small|" & vbLf & "add_textbox|This `wname `owill be reserved for you and `wshown to other players`o, so choose carefully!|" & vbLf & "add_text_input|username|GrowID||30|" & vbLf & "add_text_input|password|Password||100|" & vbLf & "add_text_input|passwordverify|Password Verify||100|" & vbLf & "add_textbox|Your `wemail address `owill only be used for account verification purposes and won't be spammed or shared. If you use a fake email, you'll never be able to recover or change your password.|" & vbLf & "add_text_input|email|Email||100|" & vbLf & "add_textbox|Your `wDiscord ID `owill be used for secondary verification if you lost access to your `wemail address`o! Please enter in such format: `wdiscordname#tag`o. Your `wDiscord Tag `ocan be found in your `wDiscord account settings`o.|" & vbLf & "add_text_input|discord|Discord||100|" & vbLf & "end_dialog|register|Cancel|Get My GrowID!|" & vbLf))
                                                                          peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                      End If

                                                                      If cch.IndexOf("action|store") = 0 Then
                                                                          Dim p2 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnStoreRequest"), "set_description_text|Welcome to the `2Growtopia Store``!  Tap the item you'd like more info on.`o  `wWant to get `5Supporter`` status? Any Gem purchase (or `57,000`` Gems earned with free `5Tapjoy`` offers) will make you one. You'll get new skin colors, the `5Recycle`` tool to convert unwanted items into Gems, and more bonuses!" & vbLf & "add_button|iap_menu|Buy Gems|interface/large/store_buttons5.rttex||0|2|0|0||" & vbLf & "add_button|subs_menu|Subscriptions|interface/large/store_buttons22.rttex||0|1|0|0||" & vbLf & "add_button|token_menu|Growtoken Items|interface/large/store_buttons9.rttex||0|0|0|0||" & vbLf & "add_button|pristine_forceps|`oAnomalizing Pristine Bonesaw``|interface/large/store_buttons20.rttex|Built to exacting specifications by GrowTech engineers to find and remove temporal anomalies from infected patients, and with even more power than Delicate versions! Note : The fragile anomaly - seeking circuitry in these devices is prone to failure and may break (though with less of a chance than a Delicate version)! Use with care!|0|3|3500|0||" & vbLf & "add_button|itemomonth|`oItem Of The Month``|interface/large/store_buttons16.rttex|`2September 2018:`` `9Sorcerer's Tunic of Mystery!`` Capable of reflecting the true colors of the world around it, this rare tunic is made of captured starlight and aether. If you think knitting with thread is hard, just try doing it with moonbeams and magic! The result is worth it though, as these clothes won't just make you look amazing - you'll be able to channel their inherent power into blasts of cosmic energy!``|0|3|200000|0||" & vbLf & "add_button|contact_lenses|`oContact Lens Pack``|interface/large/store_buttons22.rttex|Need a colorful new look? This pack includes 10 random Contact Lens colors (and may include Contact Lens Cleaning Solution, to return to your natural eye color)!|0|7|15000|0||" & vbLf & "add_button|locks_menu|Locks And Stuff|interface/large/store_buttons3.rttex||0|4|0|0||" & vbLf & "add_button|itempack_menu|Item Packs|interface/large/store_buttons3.rttex||0|3|0|0||" & vbLf & "add_button|bigitems_menu|Awesome Items|interface/large/store_buttons4.rttex||0|6|0|0||" & vbLf & "add_button|weather_menu|Weather Machines|interface/large/store_buttons5.rttex|Tired of the same sunny sky?  We offer alternatives within...|0|4|0|0||" & vbLf))
                                                                          peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                      End If

                                                                      If cch.IndexOf("action|info") = 0 Then
                                                                          Dim id As Integer = -1
                                                                          Dim count As Integer = -1

                                                                          For Each [to] As String In cch.Split(vbLf.ToCharArray())
                                                                              Dim infoDat As String() = explode("|", [to])

                                                                              If infoDat.Length = 3 Then
                                                                                  If infoDat(1) = "itemID" Then id = Convert.ToInt32(infoDat(2))
                                                                                  If infoDat(1) = "count" Then count = Convert.ToInt32(infoDat(2))
                                                                              End If
                                                                          Next

                                                                          If id = -1 OrElse count = -1 Then Return
                                                                          If itemDefs.Length < id OrElse id < 0 Then Return
                                                                          Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o" & vbLf & vbLf & "add_label_with_icon|big|`w" & itemDefs(id).name & "``|left|" & id & "|" & vbLf & vbLf & "add_spacer|small|" & vbLf & "add_textbox|" & itemDefs(id).description & "|left|" & vbLf & "add_spacer|small|" & vbLf & "add_quick_exit|" & vbLf & "add_button|chc0|Close|noflags|0|0|" & vbLf & "nend_dialog|gazette||OK|"))
                                                                          peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                      End If

                                                                      If cch.IndexOf("action|dialog_return") = 0 Then
                                                                          Dim btn As String = ""
                                                                          Dim isRegisterDialog As Boolean = False
                                                                          Dim username As String = ""
                                                                          Dim password As String = ""
                                                                          Dim passwordverify As String = ""
                                                                          Dim email As String = ""
                                                                          Dim discord As String = ""

                                                                          For Each [to] As String In cch.Split(vbLf.ToCharArray())
                                                                              Dim infoDat As String() = explode("|", [to])

                                                                              If infoDat.Length = 2 Then
                                                                                  If infoDat(0) = "buttonClicked" Then btn = infoDat(1)

                                                                                  If infoDat(0) = "dialog_name" AndAlso infoDat(1) = "register" Then
                                                                                      isRegisterDialog = True
                                                                                  End If

                                                                                  If isRegisterDialog Then
                                                                                      If infoDat(0) = "username" Then username = infoDat(1)
                                                                                      If infoDat(0) = "password" Then password = infoDat(1)
                                                                                      If infoDat(0) = "passwordverify" Then passwordverify = infoDat(1)
                                                                                      If infoDat(0) = "email" Then email = infoDat(1)
                                                                                      If infoDat(0) = "discord" Then discord = infoDat(1)
                                                                                  End If
                                                                              End If
                                                                          Next

                                                                          If btn = "worldPublic" Then
                                                                              If (TryCast(peer.Data, PlayerInfo)).rawName = getPlyersWorld(peer).owner Then getPlyersWorld(peer).isPublic = True
                                                                          End If

                                                                          If btn = "worldPrivate" Then
                                                                              If (TryCast(peer.Data, PlayerInfo)).rawName = getPlyersWorld(peer).owner Then getPlyersWorld(peer).isPublic = False
                                                                          End If

                                                                          If isRegisterDialog Then
                                                                              Dim regState As Integer = PlayerDB.playerRegister(username, password, passwordverify, email, discord)

                                                                              If regState = 1 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rYour account has been created!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  Dim p2 As GamePacket = packetEnd(appendString(appendString(appendInt(appendString(createPacket(), "SetHasGrowID"), 1), username), password))
                                                                                  peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                                  peer.DisconnectLater(0)
                                                                              ElseIf regState = -1 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rAccount creation has failed, because it already exists!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              ElseIf regState = -2 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rAccount creation has failed, because the name is too short!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              ElseIf regState = -3 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`4Passwords mismatch!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              ElseIf regState = -4 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`4Account creation has failed, because email address is invalid!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              ElseIf regState = -5 Then
                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`4Account creation has failed, because Discord ID is invalid!``"))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              End If
                                                                          End If
                                                                      End If

                                                                      Dim dropText As String = "action|drop" & vbLf & "|itemID|"

                                                                      If cch.IndexOf(dropText) = 0 Then
                                                                          sendDrop(peer, -1, (TryCast(peer.Data, PlayerInfo)).x + (32 * (If((TryCast(peer.Data, PlayerInfo)).isRotatedLeft, -1, 1))), (TryCast(peer.Data, PlayerInfo)).y, Convert.ToInt32(cch.Substring(dropText.Length, cch.Length - dropText.Length - 1)), 1, 0)
                                                                      End If

                                                                      If cch.Contains("text|") Then
                                                                          Dim str As String = cch.Substring(cch.IndexOf("text|") + 5, cch.Length - cch.IndexOf("text|") - 1)

                                                                          If str = "/mod" Then
                                                                              (TryCast(peer.Data, PlayerInfo)).canWalkInBlocks = True
                                                                              sendState(peer)
                                                                          ElseIf str.Substring(0, 7) = "/state " Then
                                                                              Dim data As PlayerMoving
                                                                              data.packetType = &H14
                                                                              data.characterState = &H0
                                                                              data.x = 1000
                                                                              data.y = 0
                                                                              data.punchX = 0
                                                                              data.punchY = 0
                                                                              data.XSpeed = 300
                                                                              data.YSpeed = 600
                                                                              data.netID = (TryCast(peer.Data, PlayerInfo)).netID
                                                                              data.plantingTree = Convert.ToInt32(str.Substring(7, cch.Length - 7 - 1))
                                                                              SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0)
                                                                          ElseIf str = "/help" Then
                                                                              Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Supported commands are: /help, /mod, /unmod, /inventory, /item id, /team id, /color number, /who, /state number, /count, /sb message, /alt, /radio, /gem"))
                                                                              peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                          ElseIf str.Substring(0, 5) = "/gem " Then
                                                                              Dim p As GamePacket = packetEnd(appendInt(appendString(createPacket(), "OnSetBux"), Convert.ToInt32(str.Substring(5))))
                                                                              peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              Return
                                                                          ElseIf str.Substring(0, 9) = "/weather " Then

                                                                              If world.name <> "ADMIN" Then

                                                                                  If world.owner <> "" Then

                                                                                      If (TryCast(peer.Data, PlayerInfo)).rawName = world.owner OrElse isSuperAdmin((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) Then

                                                                                          For Each currentPeer As ENetPeer In peers
                                                                                              If currentPeer.State <> ENetPeerState.Connected Then Continue For

                                                                                              If isHere(peer, currentPeer) Then
                                                                                                  Dim p1 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`oPlayer `2" & (TryCast(peer.Data, PlayerInfo)).displayName & "`o has just changed the world's weather!"))
                                                                                                  currentPeer.Send(p1.data, 0, ENetPacketFlags.Reliable)
                                                                                                  Dim p2 As GamePacket = packetEnd(appendInt(appendString(createPacket(), "OnSetCurrentWeather"), Convert.ToInt32(str.Substring(9))))
                                                                                                  currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                                              End If
                                                                                          Next
                                                                                      End If
                                                                                  End If
                                                                              End If
                                                                          ElseIf str = "/count" Then
                                                                              Dim count As Integer = 0
                                                                              Dim name As String = ""

                                                                              For Each currentPeer As ENetPeer In peers
                                                                                  If currentPeer.State <> ENetPeerState.Connected Then Continue For
                                                                                  count += 1
                                                                              Next

                                                                              Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "There are " & count & " people online out of 1024 limit."))
                                                                              peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                          ElseIf str.Substring(0, 5) = "/asb " Then
                                                                              If Not canSB((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) Then Return
                                                                              Console.WriteLine("ASB from " & (TryCast(peer.Data, PlayerInfo)).rawName & " in world " & (TryCast(peer.Data, PlayerInfo)).currentWorld & " with IP " + peer.RemoteEndPoint.ToString() & " with message " & str.Substring(5, cch.Length - 5 - 1))
                                                                              Dim p As GamePacket = packetEnd(appendInt(appendString(appendString(appendString(appendString(createPacket(), "OnAddNotification"), "interface/atomic_button.rttex"), str.Substring(4, cch.Length - 4 - 1)), "audio/hub_open.wav"), 0))

                                                                              For Each currentPeer As ENetPeer In peers
                                                                                  If currentPeer.State <> ENetPeerState.Connected Then Continue For
                                                                                  currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              Next
                                                                          ElseIf str.Substring(0, 4) = "/sb " Then
                                                                              Dim time As Long = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

                                                                              If (TryCast(peer.Data, PlayerInfo)).lastSB + 45000 < time Then
                                                                                  (TryCast(peer.Data, PlayerInfo)).lastSB = time
                                                                              Else
                                                                                  Dim p1 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Wait a minute before using the SB command again!"))
                                                                                  peer.Send(p1.data, 0, ENetPacketFlags.Reliable)
                                                                                  Return
                                                                              End If

                                                                              Dim name As String = (TryCast(peer.Data, PlayerInfo)).displayName
                                                                              Dim p2 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`w** `5Super-Broadcast`` from `$`2" & name & "```` (in `$" & (TryCast(peer.Data, PlayerInfo)).currentWorld & "``) ** :`` `# " & str.Substring(4, cch.Length - 4 - 1)))
                                                                              Dim text As String = "action|play_sfx" & vbLf & "file|audio/beep.wav" & vbLf & "delayMS|0" & vbLf
                                                                              Dim data As Byte() = New Byte(5 + text.Length - 1) {}
                                                                              Dim zero As Integer = 0
                                                                              Dim type As Integer = 3
                                                                              Array.Copy(BitConverter.GetBytes(type), 0, data, 0, 4)
                                                                              Array.Copy(Encoding.ASCII.GetBytes(text), 0, data, 4, text.Length)
                                                                              Array.Copy(BitConverter.GetBytes(zero), 0, data, 4 + text.Length, 1)

                                                                              For Each currentPeer As ENetPeer In peers
                                                                                  If currentPeer.State <> ENetPeerState.Connected Then Continue For
                                                                                  If Not (TryCast(peer.Data, PlayerInfo)).radio Then Continue For
                                                                                  currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                                  currentPeer.Send(data, 0, ENetPacketFlags.Reliable)
                                                                              Next
                                                                          ElseIf str.Substring(0, 6) = "/radio" Then
                                                                              Dim p As GamePacket

                                                                              If (TryCast(peer.Data, PlayerInfo)).radio Then
                                                                                  p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "You won't see broadcasts anymore."))
                                                                                  (TryCast(peer.Data, PlayerInfo)).radio = False
                                                                              Else
                                                                                  p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "You will now see broadcasts again."))
                                                                                  (TryCast(peer.Data, PlayerInfo)).radio = True
                                                                              End If

                                                                              peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                          ElseIf str.Substring(0, 6) = "/reset" Then
                                                                              If Not isSuperAdmin((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) Then Exit Select
                                                                              Console.WriteLine("Restart from " & (TryCast(peer.Data, PlayerInfo)).displayName)
                                                                              Dim p As GamePacket = packetEnd(appendInt(appendString(appendString(appendString(appendString(createPacket(), "OnAddNotification"), "interface/science_button.rttex"), "Restarting soon!"), "audio/mp3/suspended.mp3"), 0))

                                                                              For Each currentPeer As ENetPeer In peers
                                                                                  If currentPeer.State <> ENetPeerState.Connected Then Continue For
                                                                                  currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                              Next
                                                                          ElseIf str = "/unmod" Then
                                                                              (TryCast(peer.Data, PlayerInfo)).canWalkInBlocks = False
                                                                              sendState(peer)
                                                                          ElseIf str = "/alt" Then
                                                                              Dim p2 As GamePacket = packetEnd(appendInt(appendString(createPacket(), "OnSetBetaMode"), 1))
                                                                              peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                          ElseIf str = "/inventory" Then
                                                                              sendInventory(peer, (TryCast(peer.Data, PlayerInfo)).inventory)
                                                                          ElseIf str.Substring(0, 6) = "/item " Then
                                                                              Dim inventory As PlayerInventory = New PlayerInventory()
                                                                              Dim item As InventoryItem
                                                                              item.itemID = Convert.ToInt16(str.Substring(6, cch.Length - 6 - 1))
                                                                              item.itemCount = 200
                                                                              inventory.items = inventory.items.Append(item).ToArray()
                                                                              item.itemCount = 1
                                                                              item.itemID = 18
                                                                              inventory.items = inventory.items.Append(item).ToArray()
                                                                              item.itemID = 32
                                                                              inventory.items = inventory.items.Append(item).ToArray()
                                                                              sendInventory(peer, inventory)
                                                                          ElseIf str.Substring(0, 6) = "/team " Then
                                                                              Dim val As Integer = 0
                                                                              val = Convert.ToInt32(str.Substring(6, cch.Length - 6 - 1))
                                                                              Dim data As PlayerMoving
                                                                              data.packetType = &H1B
                                                                              data.characterState = &H0
                                                                              data.x = 0
                                                                              data.y = 0
                                                                              data.punchX = val
                                                                              data.punchY = 0
                                                                              data.XSpeed = 0
                                                                              data.YSpeed = 0
                                                                              data.netID = (TryCast(peer.Data, PlayerInfo)).netID
                                                                              data.plantingTree = 0
                                                                              SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0)
                                                                          ElseIf str.Substring(0, 7) = "/color " Then
                                                                              (TryCast(peer.Data, PlayerInfo)).skinColor = Convert.ToUInt32(str.Substring(6, cch.Length - 6 - 1))
                                                                              sendClothes(peer)
                                                                          End If

                                                                          If str.Substring(0, 4) = "/who" Then
                                                                              sendWho(peer)
                                                                          End If

                                                                          If str.Length <> 0 AndAlso str(0) = "/"c Then
                                                                              sendAction(peer, (TryCast(peer.Data, PlayerInfo)).netID, str)
                                                                          ElseIf str.Length > 0 Then
                                                                              sendChatMessage(peer, (TryCast(peer.Data, PlayerInfo)).netID, str)
                                                                          End If
                                                                      End If

                                                                      If Not (TryCast(peer.Data, PlayerInfo)).isIn Then
                                                                          Dim p As GamePacket = packetEnd(appendString(appendString(appendString(appendString(appendInt(appendString(createPacket(), "OnSuperMainStartAcceptLogonHrdxs47254722215a"), -1054420378), "cdn.growtopiagame.com"), "cache/"), "cc.cz.madkite.freedom org.aqua.gg idv.aqua.bulldog com.cih.gamecih2 com.cih.gamecih com.cih.game_cih cn.maocai.gamekiller com.gmd.speedtime org.dax.attack com.x0.strai.frep com.x0.strai.free org.cheatengine.cegui org.sbtools.gamehack com.skgames.traffikrider org.sbtoods.gamehaca com.skype.ralder org.cheatengine.cegui.xx.multi1458919170111 com.prohiro.macro me.autotouch.autotouch com.cygery.repetitouch.free com.cygery.repetitouch.pro com.proziro.zacro com.slash.gamebuster"), "proto=42|choosemusic=audio/mp3/about_theme.mp3|active_holiday=0|"))
                                                                          peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                          Dim str As String() = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()).Split(vbLf.ToCharArray())

                                                                          For Each [to] As String In str
                                                                              If [to] = "" Then Continue For
                                                                              Dim id As String = [to].Substring(0, [to].IndexOf("|"))
                                                                              Dim act As String = [to].Substring([to].IndexOf("|") + 1, [to].Length - [to].IndexOf("|") - 1)

                                                                              If id = "tankIDName" Then
                                                                                  (TryCast(peer.Data, PlayerInfo)).tankIDName = act
                                                                                  (TryCast(peer.Data, PlayerInfo)).haveGrowId = True
                                                                              ElseIf id = "tankIDPass" Then
                                                                                  (TryCast(peer.Data, PlayerInfo)).tankIDPass = act
                                                                              ElseIf id = "requestedName" Then
                                                                                  (TryCast(peer.Data, PlayerInfo)).requestedName = act
                                                                              ElseIf id = "country" Then
                                                                                  (TryCast(peer.Data, PlayerInfo)).country = act
                                                                              End If
                                                                          Next

                                                                          If Not (TryCast(peer.Data, PlayerInfo)).haveGrowId Then
                                                                              (TryCast(peer.Data, PlayerInfo)).rawName = ""
                                                                              (TryCast(peer.Data, PlayerInfo)).displayName = "Fake " & PlayerDB.fixColors((TryCast(peer.Data, PlayerInfo)).requestedName.Substring(0, If((TryCast(peer.Data, PlayerInfo)).requestedName.Length > 15, 15, (TryCast(peer.Data, PlayerInfo)).requestedName.Length)))
                                                                          Else
                                                                              (TryCast(peer.Data, PlayerInfo)).rawName = PlayerDB.getProperName((TryCast(peer.Data, PlayerInfo)).tankIDName)
                                                                              Dim logStatus As Integer = PlayerDB.playerLogin(peer, (TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass)

                                                                              If logStatus = 1 Then
                                                                                  Dim p1 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rYou have successfully logged into your account!``"))
                                                                                  peer.Send(p1.data, 0, ENetPacketFlags.Reliable)
                                                                                  (TryCast(peer.Data, PlayerInfo)).displayName = (TryCast(peer.Data, PlayerInfo)).tankIDName
                                                                              Else
                                                                                  Dim p1 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`rWrong username or password!``"))
                                                                                  peer.Send(p1.data, 0, ENetPacketFlags.Reliable)
                                                                                  peer.DisconnectLater(0)
                                                                                  Return
                                                                              End If
                                                                          End If

                                                                          For Each c As Char In (TryCast(peer.Data, PlayerInfo)).displayName
                                                                              If c < &H20 OrElse c > &H7A Then (TryCast(peer.Data, PlayerInfo)).displayName = "Bad characters in name, remove them!"
                                                                          Next

                                                                          If (TryCast(peer.Data, PlayerInfo)).country.Length > 4 Then
                                                                              (TryCast(peer.Data, PlayerInfo)).country = "us"
                                                                          End If

                                                                          If getAdminLevel((TryCast(peer.Data, PlayerInfo)).rawName, (TryCast(peer.Data, PlayerInfo)).tankIDPass) > 0 Then
                                                                              (TryCast(peer.Data, PlayerInfo)).country = "../cash_icon_overlay"
                                                                          End If

                                                                          Dim p2 As GamePacket = packetEnd(appendString(appendString(appendInt(appendString(createPacket(), "SetHasGrowID"), (If((TryCast(peer.Data, PlayerInfo)).haveGrowId, 1, 0))), (TryCast(peer.Data, PlayerInfo)).tankIDName), (TryCast(peer.Data, PlayerInfo)).tankIDPass))
                                                                          peer.Send(p2.data, 0, ENetPacketFlags.Reliable)
                                                                      End If

                                                                      Dim pStr As String = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray())

                                                                      If pStr.Contains("action|enter_game") AndAlso Not (TryCast(peer.Data, PlayerInfo)).isIn Then
                                                                          Console.WriteLine("And we are in!")
                                                                          (TryCast(peer.Data, PlayerInfo)).isIn = True
                                                                          sendWorldOffers(peer)
                                                                          Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "C# Server made by willi123yao."))
                                                                          peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                          Dim inventory As PlayerInventory = New PlayerInventory()

                                                                          For i As Integer = 0 To 200 - 1
                                                                              Dim it As InventoryItem = New InventoryItem()
                                                                              it.itemID = CShort(((i * 2) + 2))
                                                                              it.itemCount = 200
                                                                              inventory.items = inventory.items.Append(it).ToArray()
                                                                          Next

                                                                          (TryCast(peer.Data, PlayerInfo)).inventory = inventory

                                                                          If True Then
                                                                              Dim p4 As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o" & vbLf & vbLf & "add_label_with_icon|big|`wThe Growtopia Gazette``|left|5016|" & vbLf & vbLf & "add_spacer|small|" & vbLf & "add_label_with_icon|small|`4WARNING:`` `5Worlds (and accounts)`` might be deleted at any time if database issues appear (once per day or week).|left|4|" & vbLf & "add_label_with_icon|small|`4WARNING:`` `5Accounts`` are in beta, bugs may appear and they will be probably deleted often, because of new account updates, which will cause database incompatibility.|left|4|" & vbLf & "add_spacer|small|" & vbLf & vbLf & "add_url_button||``Watch: `1Watch a video about GT Private Server``|NOFLAGS|https://www.youtube.com/watch?v=_3avlDDYBBY|Open link?|0|0|" & vbLf & "add_url_button||``Channel: `1Watch Growtopia Noobs' channel``|NOFLAGS|https://www.youtube.com/channel/UCLXtuoBlrXFDRtFU8vPy35g|Open link?|0|0|" & vbLf & "add_url_button||``Items: `1Item database by Nenkai``|NOFLAGS|https://raw.githubusercontent.com/Nenkai/GrowtopiaItemDatabase/master/GrowtopiaItemDatabase/CoreData.txt|Open link?|0|0|" & vbLf & "add_url_button||``Discord: `1GT Private Server Discord``|NOFLAGS|https://discord.gg/8WUTs4v|Open the link?|0|0|" & vbLf & "add_quick_exit|" & vbLf & "add_button|chc0|Close|noflags|0|0|" & vbLf & "nend_dialog|gazette||OK|"))
                                                                              peer.Send(p4.data, 0, ENetPacketFlags.Reliable)
                                                                          End If
                                                                      End If

                                                                      If Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()) = "action|refresh_item_data" & vbLf Then

                                                                          If itemsDat IsNot Nothing Then
                                                                              peer.Send(itemsDat, 0, ENetPacketFlags.Reliable)
                                                                              (TryCast(peer.Data, PlayerInfo)).isUpdating = True
                                                                              peer.DisconnectLater(0)
                                                                          End If
                                                                      End If

                                                                      Exit Select
                                                                  Case 3
                                                                      Dim isJoinReq As Boolean = False

                                                                      For Each [to] As String In Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()).Split(vbLf.ToCharArray())
                                                                          If [to] = "" Then Continue For
                                                                          Dim id As String = [to].Substring(0, [to].IndexOf("|"))
                                                                          Dim act As String = [to].Substring([to].IndexOf("|") + 1, [to].Length - [to].IndexOf("|") - 1)

                                                                          If id = "name" AndAlso isJoinReq Then
                                                                              Console.WriteLine("Entering some world...")

                                                                              Try
                                                                                  Dim info As WorldInfo = worldDB.[get](act)
                                                                                  sendWorld(peer, info)
                                                                                  Dim x As Integer = 3040
                                                                                  Dim y As Integer = 736

                                                                                  For j As Integer = 0 To info.width * info.height - 1

                                                                                      If info.items(j).foreground = 6 Then
                                                                                          x = (j Mod info.width) * 32
                                                                                          y = (j / info.width) * 32
                                                                                      End If
                                                                                  Next

                                                                                  Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnSpawn"), "spawn|avatar" & vbLf & "netID|" & cId & vbLf & "userID|" & cId & vbLf & "colrect|0|0|20|30" & vbLf & "posXY|" & x & "|" & y & vbLf & "name|``" & (TryCast(peer.Data, PlayerInfo)).displayName & "``" & vbLf & "country|" & (TryCast(peer.Data, PlayerInfo)).country & vbLf & "invis|0" & vbLf & "mstate|0" & vbLf & "smstate|0" & vbLf & "type|local" & vbLf))
                                                                                  peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  (TryCast(peer.Data, PlayerInfo)).netID = cId
                                                                                  onPeerConnect(peer)
                                                                                  cId += 1
                                                                                  sendInventory(peer, (TryCast(peer.Data, PlayerInfo)).inventory)
                                                                              Catch
                                                                                  Dim e As Integer = 0

                                                                                  If e = 1 Then
                                                                                      (TryCast(peer.Data, PlayerInfo)).currentWorld = "EXIT"
                                                                                      Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "You have exited the world."))
                                                                                      peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  ElseIf e = 2 Then
                                                                                      (TryCast(peer.Data, PlayerInfo)).currentWorld = "EXIT"
                                                                                      Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "You have entered bad characters in the world name!"))
                                                                                      peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  ElseIf e = 3 Then
                                                                                      (TryCast(peer.Data, PlayerInfo)).currentWorld = "EXIT"
                                                                                      Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Exit from what? Click back if you're done playing."))
                                                                                      peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  Else
                                                                                      (TryCast(peer.Data, PlayerInfo)).currentWorld = "EXIT"
                                                                                      Dim p As GamePacket = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "I know this menu is magical and all, but it has its limitations! You can't visit this world!"))
                                                                                      peer.Send(p.data, 0, ENetPacketFlags.Reliable)
                                                                                  End If
                                                                              End Try
                                                                          End If

                                                                          If id = "action" Then

                                                                              If act = "join_request" Then
                                                                                  isJoinReq = True
                                                                              End If

                                                                              If act = "quit_to_exit" Then
                                                                                  sendPlayerLeave(peer, TryCast(peer.Data, PlayerInfo))
                                                                                  (TryCast(peer.Data, PlayerInfo)).currentWorld = "EXIT"
                                                                                  sendWorldOffers(peer)
                                                                              End If

                                                                              If act = "quit" Then
                                                                                  peer.DisconnectLater(0)
                                                                              End If
                                                                          End If
                                                                      Next

                                                                      Exit Select
                                                                  Case 4

                                                                      If True Then
                                                                          Dim tankUpdatePacket As Byte() = pak.Skip(4).ToArray()

                                                                          If tankUpdatePacket.Length <> 0 Then
                                                                              Dim pMov As PlayerMoving = unpackPlayerMoving(tankUpdatePacket)

                                                                              Select Case pMov.packetType
                                                                                  Case 0
                                                                                      (TryCast(peer.Data, PlayerInfo)).x = CInt(pMov.x)
                                                                                      (TryCast(peer.Data, PlayerInfo)).y = CInt(pMov.y)
                                                                                      (TryCast(peer.Data, PlayerInfo)).isRotatedLeft = (pMov.characterState And &H10) <> 0
                                                                                      sendPData(peer, pMov)

                                                                                      If Not (TryCast(peer.Data, PlayerInfo)).joinClothesUpdated Then
                                                                                          (TryCast(peer.Data, PlayerInfo)).joinClothesUpdated = True
                                                                                          updateAllClothes(peer)
                                                                                      End If

                                                                                  Case Else
                                                                              End Select

                                                                              Dim data2 As PlayerMoving = unpackPlayerMoving(tankUpdatePacket)

                                                                              If data2.packetType = 11 Then
                                                                              End If

                                                                              If data2.packetType = 7 Then
                                                                                  sendWorldOffers(peer)
                                                                              End If

                                                                              If data2.packetType = 10 Then
                                                                                  Dim def As ItemDefinition = New ItemDefinition()

                                                                                  Try
                                                                                      def = getItemDef(pMov.plantingTree)
                                                                                  Catch
                                                                                  End Try

                                                                                  Select Case def.clothType
                                                                                      Case ClothTypes.HAIR

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_hair = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_hair = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_hair = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.SHIRT

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_shirt = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_shirt = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_shirt = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.PANTS

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_pants = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_pants = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_pants = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.FEET

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_feet = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_feet = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_feet = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.FACE

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_face = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_face = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_face = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.HAND

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_hand = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_hand = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_hand = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.BACK

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_back = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_back = 0
                                                                                              (TryCast(peer.Data, PlayerInfo)).canDoubleJump = False
                                                                                              sendState(peer)
                                                                                              Exit Select
                                                                                          End If

                                                                                          If True Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_back = pMov.plantingTree
                                                                                              Dim item As Integer = pMov.plantingTree

                                                                                              If item = 156 OrElse item = 362 OrElse item = 678 OrElse item = 736 OrElse item = 818 OrElse item = 1206 OrElse item = 1460 OrElse item = 1550 OrElse item = 1574 OrElse item = 1668 OrElse item = 1672 OrElse item = 1674 OrElse item = 1784 OrElse item = 1824 OrElse item = 1936 OrElse item = 1938 OrElse item = 1970 OrElse item = 2254 OrElse item = 2256 OrElse item = 2258 OrElse item = 2260 OrElse item = 2262 OrElse item = 2264 OrElse item = 2390 OrElse item = 2392 OrElse item = 3120 OrElse item = 3308 OrElse item = 3512 OrElse item = 4534 OrElse item = 4986 OrElse item = 5754 OrElse item = 6144 OrElse item = 6334 OrElse item = 6694 OrElse item = 6818 OrElse item = 6842 OrElse item = 1934 OrElse item = 3134 OrElse item = 6004 OrElse item = 1780 OrElse item = 2158 OrElse item = 2160 OrElse item = 2162 OrElse item = 2164 OrElse item = 2166 OrElse item = 2168 OrElse item = 2438 OrElse item = 2538 OrElse item = 2778 OrElse item = 3858 OrElse item = 350 OrElse item = 998 OrElse item = 1738 OrElse item = 2642 OrElse item = 2982 OrElse item = 3104 OrElse item = 3144 OrElse item = 5738 OrElse item = 3112 OrElse item = 2722 OrElse item = 3114 OrElse item = 4970 OrElse item = 4972 OrElse item = 5020 OrElse item = 6284 OrElse item = 4184 OrElse item = 4628 OrElse item = 5322 OrElse item = 4112 OrElse item = 4114 OrElse item = 3442 Then
                                                                                                  (TryCast(peer.Data, PlayerInfo)).canDoubleJump = True
                                                                                              Else
                                                                                                  (TryCast(peer.Data, PlayerInfo)).canDoubleJump = False
                                                                                              End If

                                                                                              sendState(peer)
                                                                                          End If

                                                                                          Exit Select
                                                                                      Case ClothTypes.MASK

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_mask = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_mask = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_mask = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case ClothTypes.NECKLACE

                                                                                          If (TryCast(peer.Data, PlayerInfo)).cloth_necklace = pMov.plantingTree Then
                                                                                              (TryCast(peer.Data, PlayerInfo)).cloth_necklace = 0
                                                                                              Exit Select
                                                                                          End If

                                                                                          (TryCast(peer.Data, PlayerInfo)).cloth_necklace = pMov.plantingTree
                                                                                          Exit Select
                                                                                      Case Else
                                                                                          Console.WriteLine("Invalid item activated: " & pMov.plantingTree & " by " & (TryCast(peer.Data, PlayerInfo)).displayName)
                                                                                          Exit Select
                                                                                  End Select

                                                                                  sendClothes(peer)
                                                                              End If

                                                                              If data2.packetType = 18 Then
                                                                                  sendPData(peer, pMov)
                                                                              End If

                                                                              If data2.punchX <> -1 AndAlso data2.punchY <> -1 Then

                                                                                  If data2.packetType = 3 Then
                                                                                      sendTileUpdate(data2.punchX, data2.punchY, data2.plantingTree, (TryCast(peer.Data, PlayerInfo)).netID, peer)
                                                                                  End If
                                                                              End If
                                                                          Else
                                                                              Console.WriteLine("Got bad tank packet")
                                                                          End If
                                                                      End If

                                                                  Case 5
                                                                  Case 6
                                                                  Case Else
                                                                      Console.WriteLine("Unknown packet type " & messageType)
                                                              End Select
                                                          End Function

                                    eve.Peer.OnDisconnect += Function(ByVal send As Object, ByVal ev As UInteger)
                                                                 Dim peer As ENetPeer = TryCast(send, ENetPeer)
                                                                 Console.WriteLine("Peer disconnected")
                                                                 sendPlayerLeave(peer, TryCast(peer.Data, PlayerInfo))
                                                                 (TryCast(peer.Data, PlayerInfo)).inventory.items = New InventoryItem() {}
                                                                 peer.Data = Nothing
                                                                 peers.Remove(peer)
                                                             End Function
                                End Function

            server.StartServiceThread()
            Thread.Sleep(Timeout.Infinite)
            Console.WriteLine("Program ended??? Huh?")
        End Sub
    End Class
End Namespace
